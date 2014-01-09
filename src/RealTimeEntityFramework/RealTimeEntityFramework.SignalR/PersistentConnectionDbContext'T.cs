using System.Data.Common;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace RealTimeEntityFramework.SignalR
{
    public class PersistentConnectionDbContext<TConnection> : SignalRDbContext
        where TConnection : PersistentConnection
    {
        private IPersistentConnectionContext _connectionContext;

        public PersistentConnectionDbContext()
            : base()
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(DbCompiledModel model)
            : base(model)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Initialize(GlobalHost.ConnectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager)
            : base()
        {
            Initialize(connectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager, string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Initialize(connectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager, DbCompiledModel model)
            : base(model)
        {
            Initialize(connectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager, string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Initialize(connectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager, DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Initialize(connectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager, ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Initialize(connectionManager);
        }

        public PersistentConnectionDbContext(IConnectionManager connectionManager, DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Initialize(connectionManager);
        }

        protected override void OnChange(string groupName, ChangeNotification change)
        {
            _connectionContext.Groups.Send(groupName, change);
        }

        private void Initialize(IConnectionManager connectionManager)
        {
            _connectionContext = connectionManager.GetConnectionContext<TConnection>();
        }
    }
}
