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
using System.Text;
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

        public IEnumerable<TEntity> Select<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var result = entities.Where(predicate);

            AddToNotificationGroup(hubContext.ConnectionId, result, predicate);

            return result;
        }

        public Task AddToNotificationGroup<TEntity>(string connectionId, TEntity entity)
        {
            var group = GetPrimaryKeyNotificationGroupName(entity);

            return _hubContext.Groups.Add(connectionId, group);
        }

        public Task AddToNotificationGroup<TEntity>(string connectionId, IEnumerable<TEntity> entities, Expression<Func<TEntity, bool>> predicate)
        {
            var group = GetPredicateNotificationGroupName(predicate);

            return _hubContext.Groups.Add(connectionId, group);
        }

        public string GetPrimaryKeyNotificationGroupName<TEntity>(TEntity entity)
        {
            var entityKey = GetEntityKey(entity);

            if (entityKey != null)
            {
                // Build group name for entity key, format: [EntityTypeName]_PrimaryKey_[Key1Name]_[Key1Value]_[Key2Name]_[Key2Value]
                var prefix = String.Format("{0}_PrimaryKey", typeof(TEntity).FullName);
                return entityKey.EntityKeyValues.Aggregate(prefix, (name, k) => String.Format("{0}_{1}_{2}", name, k.Key, k.Value));
            }

            // TODO: No key found, should we throw here?
            return null;
        }

        public string GetPredicateNotificationGroupName<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            // TODO: Analyze the predicate and create a group name for each notification compatible condition, e.g. foreign key values

            var valueExtractor = new PredicateExpressionValueExtractor<TEntity>(this);
            valueExtractor.Visit(predicate);

            var expr = valueExtractor.EntityExpression as ConstantExpression;

            if (expr == null)
            {
                throw new InvalidOperationException("WTF");
            }

            object value = expr.Value;

            // Build group name for predicate value, format: [EntityTypeName]_Set_[FieldName]_[FieldValue]
            var groupName = String.Format("{0}_Set_{1}_{2}", typeof(TEntity).FullName, valueExtractor.EntityFieldName, value);
            return groupName;
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

                    if (IsEntityPropertyMemberExpression(node.Left))
                    {
                        target = node.Right;
                    }
                    else if (IsEntityPropertyMemberExpression(node.Right))
                    {
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

                        EntityFieldName = node.Member.Name;

                        return Expression.Constant(value);
                    }
                }

                return base.VisitMember(node);
            }

            private bool IsEntityPropertyMemberExpression(Expression expression)
            {
                if (expression.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpression = (MemberExpression)expression;
                    if (memberExpression.Member.DeclaringType == typeof(TEntity)
                        && memberExpression.Member.MemberType == MemberTypes.Property)
                    {
                        // TODO: Ensure the member being accessed is configured as a notification trigger, e.g. foregin key, explicitly decorated
                        
                        var conceptualSpaceItems = _objectContext.MetadataWorkspace.GetItems<EntityType>(DataSpace.CSpace);
                        var objectSpaceItems = (ObjectItemCollection)_objectContext.MetadataWorkspace.GetItemCollection(DataSpace.OSpace);
                        var entityMetadata = conceptualSpaceItems.FirstOrDefault(entityType =>
                        {
                            var objectSpaceType = (EntityType)_objectContext.MetadataWorkspace.GetObjectSpaceType(entityType);
                            var clrType = objectSpaceItems.GetClrType((StructuralType)objectSpaceType);
                            return clrType == typeof(TEntity);
                        });

                        var isComparingToForeignKey = entityMetadata.NavigationProperties
                            .Any(np => np.GetDependentProperties()
                                         .Any(p => String.Equals(p.Name,  memberExpression.Member.Name, StringComparison.Ordinal)));

                        return isComparingToForeignKey;
                    }
                }

                return false;
            }
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
            var payload = details.Select(c => new
            {
                changeType = c.EntityState.ToString(),
                keyNames = c.EntityKey.EntityKeyValues.Select(k => k.Key).ToList(),
                entity = c.Entity
            });

            ((IClientProxy)_hubContext.Clients.All).Invoke(ClientEntityUpdatedMethodName, payload);
        }

        private EntityKey GetEntityKey<TEntity>(TEntity entity)
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;
            var objectStateEntry = objectContext.ObjectStateManager.GetObjectStateEntry(entity);
            var entityKey = objectStateEntry != null ? objectStateEntry.EntityKey : null;

            return entityKey;
        }
    }
}
