using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace RealTimeEntityFramework.SignalR
{
    /// <summary>
    /// A DbContext base class the sends notifications to SignalR clients via implied Hub groups whenever any tracked entities are added, updated or deleted.
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public abstract class HubDbContext<THub> : SignalRDbContext
        where THub : IHub
    {
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

        //public TEntity FindWithNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, params object[] keyValues) where TEntity : class
        //{
        //    return FindWithNotifications(hubContext.ConnectionId, entities, keyValues);
        //}

        //public IQueryable<TEntity> SelectWithNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        //{
        //    return SelectWithNotifications(hubContext.ConnectionId, entities, predicate);
        //}

        //public void StartNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, params object[] keyValues) where TEntity : class
        //{
        //    StartNotifications<TEntity>(hubContext.ConnectionId, keyValues);
        //}

        //public void StartNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        //{
        //    StartNotifications<TEntity>(hubContext.ConnectionId, predicate);
        //}

        public void StartNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, object propertyMap) where TEntity : class
        {
            StartNotifications<TEntity>(hubContext.ConnectionId, propertyMap);
        }

        public void StartNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, IDictionary<string, object> properties) where TEntity : class
        {
            StartNotifications<TEntity>(hubContext.ConnectionId, properties);
        }

        //public void StopNotifications<TEntity>(HubCallerContext hubContext, params object[] keyValues) where TEntity : class
        //{
        //    StopNotifications<TEntity>(hubContext.ConnectionId, keyValues);
        //}

        //public void StopNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        //{
        //    StopNotifications<TEntity>(hubContext.ConnectionId, predicate);
        //}

        public void StopNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, object propertyMap) where TEntity : class
        {
            StopNotifications<TEntity>(hubContext.ConnectionId, propertyMap);
        }

        public void StopNotifications<TEntity>(HubCallerContext hubContext, DbSet<TEntity> entities, IDictionary<string, object> properties) where TEntity : class
        {
            StopNotifications<TEntity>(hubContext.ConnectionId, properties);
        }

        protected override void OnChange(string groupName, SignalRChangeNotification change)
        {
            ((IClientProxy)_hubContext.Clients.Group(groupName)).Invoke(ClientEntityUpdatedMethodName, change);
        }

        private void Initialize(IConnectionManager connectionManager)
        {
            ClientEntityUpdatedMethodName = "entityUpdated";

            _hubContext = connectionManager.GetHubContext<THub>();

            base.Initialize(_hubContext.Groups);
        }
    }
}
