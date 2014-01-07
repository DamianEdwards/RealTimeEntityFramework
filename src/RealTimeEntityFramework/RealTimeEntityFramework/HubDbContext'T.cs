using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace RealTimeEntityFramework
{
    /// <summary>
    /// A DbContext base class the sends notifications to SignalR clients via implied Hub groups whenever any tracked entities are added, updated or deleted.
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public abstract class HubDbContext<THub> : NotifyingDbContext where THub : IHub
    {
        private IDisposable _subscription;
        private IHubContext _hubContext;

        public HubDbContext()
            : base()
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(DbCompiledModel model)
            : base(model)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager)
            : base()
        {
            Initialize(connectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager, string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Initialize(connectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager, DbCompiledModel model)
            : base(model)
        {
            Initialize(connectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager, string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Initialize(connectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager, DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Initialize(connectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager, ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Initialize(connectionManager);
        }

        public HubDbContext(IConnectionManager connectionManager, DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Initialize(connectionManager);
        }

        public string ClientEntityUpdatedMethodName { get; set; }

        public TEntity Find<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, params object[] keyValues) where TEntity : class
        {
            var result = entities.Find(keyValues);

            AddToNotificationGroup(hubContext.ConnectionId, result);

            return result;
        }

        public IQueryable<TEntity> Select<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var result = entities.Where(predicate);

            AddToNotificationGroup(hubContext.ConnectionId, predicate);

            return result;
        }

        public Task AddToNotificationGroup<TEntity>(string connectionId, TEntity entity)
        {
            var group = GetPrimaryKeyNotificationGroupName(entity);

            return _hubContext.Groups.Add(connectionId, group);
        }

        public Task AddToNotificationGroup<TEntity>(string connectionId, Expression<Func<TEntity, bool>> predicate)
        {
            var group = GetPredicateNotificationGroupName(predicate);

            return _hubContext.Groups.Add(connectionId, group);
        }

        public string GetPrimaryKeyNotificationGroupName<TEntity>(TEntity entity)
        {
            return GetPrimaryKeyNotificationGroupName(typeof(TEntity).FullName, GetEntityKey(entity));
        }

        public string GetPrimaryKeyNotificationGroupName(string entityTypeName, EntityKey entityKey)
        {
            if (entityKey != null && entityKey.EntityKeyValues != null)
            {
                // Build group name for entity key, format: [EntityTypeName]_PrimaryKey_[Key1Name]_[Key1Value]_[Key2Name]_[Key2Value]
                var prefix = String.Format("{0}_PrimaryKey", entityTypeName);
                return entityKey.EntityKeyValues.Aggregate(prefix, (name, k) => String.Format("{0}_{1}_{2}", name, k.Key, k.Value));
            }

            return null;
        }

        public string GetPredicateNotificationGroupName<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            // TODO: Support predicates with multiple compatible conditions, which result in multiple groups, e.g. p => p.CategoryId = categoryId && p.IsVisible

            var valueExtractor = new PredicateExpressionValueExtractor<TEntity>(this);
            valueExtractor.Visit(predicate);

            var expr = valueExtractor.EntityExpression as ConstantExpression;

            if (expr == null)
            {
                throw new InvalidOperationException("The predicate provided does not support change notifications.");
            }

            object value = expr.Value;

            // Build group name for predicate value, format: [EntityTypeName]_Set_[FieldName]_[FieldValue]
            var groupName = String.Format("{0}_Set_{1}_{2}", typeof(TEntity).FullName, valueExtractor.EntityFieldName, value);
            return groupName;
        }

        public IEnumerable<string> GetForeignKeysNotificationGroupNames<TEntity>(TEntity entity)
        {


            yield return null;
        }

        protected override void Dispose(bool disposing)
        {
            _subscription.Dispose();

            base.Dispose(disposing);
        }

        private void Initialize(IConnectionManager connectionManager)
        {
            ClientEntityUpdatedMethodName = "entityUpdated";

            _hubContext = connectionManager.GetHubContext<THub>();

            _subscription = Subscribe(GetType(), Notify);
        }

        private void Notify(IEnumerable<ChangeDetails> details)
        {
            var clients = _hubContext.Clients;

            // Inspect the changes and push update notifications to generated groups
            foreach (var change in details)
            {
                string groupName;
                ChangeNotification notification;

                // Notify subscribers listening for changes to this specific entity
                if (change.EntityState != EntityState.Added)
                {
                    groupName = GetPrimaryKeyNotificationGroupName(change.Entity.GetType().FullName, change.EntityKey);
                    notification = new ChangeNotification
                    {
                        Scope = ChangeScope.SpecificEntity,
                        ChangeType = ChangeNotification.ChangeTypeFromEntityState(change.EntityState),
                        EntityType = change.Entity.GetType(),
                        EntityKeyNames = change.EntityKey.EntityKeyValues.Select(k => k.Key).ToList(),
                        EntityKeyValues = change.EntityKey.EntityKeyValues.Select(k => k.Value).ToList()
                    };
                    ((IClientProxy)clients.Group(groupName)).Invoke(ClientEntityUpdatedMethodName, notification);
                }

                // Notify subscribers listening for changes to sets which contain/contained this entity

                // Foreign keys, [EntityTypeName]_Set_[ForeignKeyName]_[KeyValue]
                var foreignKeyGroupNamePrefix = String.Format("{0}_Set", change.Entity.GetType().FullName);
                
                var entityMetadata = this.GetEntityModelMetadata(change.Entity.GetType());
                var foreignKeys = entityMetadata.NavigationProperties
                    .Select(np => np.GetDependentProperties().FirstOrDefault())
                    .Where(p => p != null);

                foreach (var key in foreignKeys)
                {
                    var keyProperty = Entry(change.Entity).Property(key.Name);
                    
                    if (keyProperty.ParentProperty != null)
                    {
                        // This is complex property so umm, yeah, no idea what we should do here :S
                        continue;
                    }

                    if (change.EntityState == EntityState.Added ||
                        change.EntityState == EntityState.Modified && change.PropertyChanges.Any(c => c.PropertyName == key.Name))
                    {
                        // The key changed or this is a new/deleted entity

                        if (change.EntityState == EntityState.Modified)
                        {
                            // Notify the group for the original set this entity was removed from
                            groupName = String.Format("{0}_{1}_{2}", foreignKeyGroupNamePrefix, keyProperty.Name, keyProperty.OriginalValue);
                            notification = new ChangeNotification
                            {
                                Scope = ChangeScope.EntitySet,
                                ChangeType = ChangeType.Deleted,
                                SourceFieldNames = new[] { key.Name },
                                EntityType = change.Entity.GetType(),
                                EntityKeyNames = change.EntityKey.EntityKeyValues.Select(k => k.Key).ToList(),
                                EntityKeyValues = change.EntityKey.EntityKeyValues.Select(k => k.Value).ToList()
                            };
                            ((IClientProxy)clients.Group(groupName)).Invoke(ClientEntityUpdatedMethodName, notification);
                        }

                        // Notify the group for the current set this entity was added to
                        groupName = String.Format("{0}_{1}_{2}", foreignKeyGroupNamePrefix, keyProperty.Name, keyProperty.CurrentValue);
                        notification = new ChangeNotification
                        {
                            Scope = ChangeScope.EntitySet,
                            ChangeType = ChangeType.Added,
                            SourceFieldNames = new[] { key.Name },
                            EntityType = change.Entity.GetType(),
                            EntityKeyNames = change.EntityKey.EntityKeyValues.Select(k => k.Key).ToList(),
                            EntityKeyValues = change.EntityKey.EntityKeyValues.Select(k => k.Value).ToList()
                        };
                        ((IClientProxy)clients.Group(groupName)).Invoke(ClientEntityUpdatedMethodName, notification);
                    }
                    else
                    {
                        // The key didn't change or the entity was deleted so just tell the current set group that an entity changed
                        groupName = String.Format("{0}_{1}_{2}", foreignKeyGroupNamePrefix, keyProperty.Name, keyProperty.CurrentValue);
                        notification = new ChangeNotification
                        {
                            Scope = ChangeScope.EntitySet,
                            ChangeType = ChangeType.Updated,
                            SourceFieldNames = new[] { key.Name },
                            EntityType = change.Entity.GetType(),
                            EntityKeyNames = change.EntityKey.EntityKeyValues.Select(k => k.Key).ToList(),
                            EntityKeyValues = change.EntityKey.EntityKeyValues.Select(k => k.Value).ToList()
                        };
                        ((IClientProxy)clients.Group(groupName)).Invoke(ClientEntityUpdatedMethodName, notification);
                    }
                }
                
                // TODO: Other fields
                
            }
        }

        private EntityKey GetEntityKey<TEntity>(TEntity entity)
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;
            var objectStateEntry = objectContext.ObjectStateManager.GetObjectStateEntry(entity);
            var entityKey = objectStateEntry != null ? objectStateEntry.EntityKey : null;

            return entityKey;
        }

        private class PredicateExpressionValueExtractor<TEntity> : ExpressionVisitor
        {
            private bool _evaluating;

            private readonly ObjectContext _objectContext;

            public PredicateExpressionValueExtractor(DbContext dbContext)
            {
                _objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            }

            public string EntityFieldName { get; private set; }
            public Expression EntityExpression { get; set; }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (_evaluating)
                {
                    // TODO: Evaluate nested binary expressions

                }
                else
                {
                    Expression target = null;

                    if (IsNotificationSupportedEntityPropertyExpression(node.Left))
                    {
                        EntityFieldName = ((MemberExpression)node.Left).Member.Name;
                        target = node.Right;
                    }
                    else if (IsNotificationSupportedEntityPropertyExpression(node.Right))
                    {
                        EntityFieldName = ((MemberExpression)node.Right).Member.Name;
                        target = node.Left;
                    }

                    if (target != null)
                    {
                        try
                        {
                            _evaluating = true;
                            EntityExpression = base.Visit(target);
                        }
                        finally
                        {
                            _evaluating = false;
                        }
                    }
                }

                return base.VisitBinary(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (_evaluating)
                {
                    if (node.Member.MemberType == MemberTypes.Field &&
                        node.Expression.NodeType == ExpressionType.Constant)
                    {
                        var value = ((FieldInfo)node.Member).GetValue(((ConstantExpression)node.Expression).Value);

                        return Expression.Constant(value);
                    }
                }

                return base.VisitMember(node);
            }

            private bool IsNotificationSupportedEntityPropertyExpression(Expression expression)
            {
                if (expression.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpression = (MemberExpression)expression;
                    if (memberExpression.Member.DeclaringType == typeof(TEntity)
                        && memberExpression.Member.MemberType == MemberTypes.Property)
                    {
                        // Get the EF meta model for the entity
                        var entityMetadata = _objectContext.GetEntityModelMetadata<TEntity>();

                        // Determine if the property being accessed is supported as a notification trigger

                        // Foreign Keys
                        // TODO: This logic needs to more complex to support multi-part foreign keys, etc.
                        var isComparingToForeignKey = entityMetadata.NavigationProperties
                            .Any(np => np.GetDependentProperties()
                                         .Any(p => String.Equals(p.Name, memberExpression.Member.Name, StringComparison.Ordinal)));

                        if (isComparingToForeignKey)
                        {
                            return true;
                        }

                        // TODO: Support other property types, e.g. primitive types that are explicitly decorated as notification triggers
                    }
                }

                return false;
            }
        }
    }
}
