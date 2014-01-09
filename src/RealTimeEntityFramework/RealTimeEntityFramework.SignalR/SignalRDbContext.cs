using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.SignalR;

namespace RealTimeEntityFramework.SignalR
{
    public abstract class SignalRDbContext : NotifyingDbContext
    {
        private IGroupManager _groupManager;

        public SignalRDbContext()
            : base()
        {
            
        }

        public SignalRDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            
        }

        public SignalRDbContext(DbCompiledModel model)
            : base(model)
        {
            
        }

        public SignalRDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            
        }

        public SignalRDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            
        }

        public SignalRDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            
        }

        public SignalRDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            
        }

        protected void Initialize(IGroupManager groupManager)
        {
            _groupManager = groupManager;
        }

        public TEntity FindWithNotifications<TEntity>(string connectionId, DbSet<TEntity> entities, params object[] keyValues) where TEntity : class
        {
            StartNotifications<TEntity>(connectionId, keyValues);

            return entities.Find(keyValues);
        }

        public IQueryable<TEntity> SelectWithNotifications<TEntity>(string connectionId, DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            StartNotifications(connectionId, predicate);

            return entities.Where(predicate);
        }

        public void StartNotifications<TEntity>(string connectionId, params object[] keyValues) where TEntity : class
        {
            var group = ChangeNotifier.GetPrimaryKeyNotificationGroupName<TEntity>(keyValues);

            _groupManager.Add(connectionId, group);
        }

        public void StartNotifications<TEntity>(string connectionId, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var groups = ChangeNotifier.GetPredicateNotificationGroupNames(predicate);

            foreach (var group in groups)
            {
                _groupManager.Add(connectionId, group);
            }
        }

        public void StopNotifications<TEntity>(string connectionId, params object[] keyValues) where TEntity : class
        {
            var group = ChangeNotifier.GetPrimaryKeyNotificationGroupName<TEntity>(keyValues);

            _groupManager.Remove(connectionId, group);
        }

        public void StopNotifications<TEntity>(string connectionId, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            var groups = ChangeNotifier.GetPredicateNotificationGroupNames(predicate);

            foreach (var group in groups)
            {
                _groupManager.Remove(connectionId, group);
            }
        }
    }
}
