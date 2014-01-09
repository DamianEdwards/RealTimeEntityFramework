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
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    /// <summary>
    /// A DbContext base class the sends notifications to subscribers whenever any tracked entities are added, updated or deleted.
    /// </summary>
    public abstract class NotifyingDbContext : DbContext, IDbContext
    {
        private ObjectContext _objectContext;
        
        public NotifyingDbContext()
            : base()
        {
            Initialize();
        }

        public NotifyingDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Initialize();
        }

        public NotifyingDbContext(DbCompiledModel model)
            : base(model)
        {
            Initialize();
        }

        public NotifyingDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Initialize();
        }

        public NotifyingDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Initialize();
        }

        public NotifyingDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Initialize();
        }

        public NotifyingDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Initialize();
        }

        protected DbContextChangeNotifier ChangeNotifier { private set; get; }

        private void Initialize()
        {
            _objectContext = ((IObjectContextAdapter)this).ObjectContext;
            ChangeNotifier = new DbContextChangeNotifier(this);
            ChangeNotifier.OnChange += OnChange;
        }

        public override int SaveChanges()
        {
            return ChangeNotifier.OnSaveChanges();
        }

        public override Task<int> SaveChangesAsync()
        {
            return SaveChangesAsync(CancellationToken.None);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return ChangeNotifier.OnSaveChangesAsync(cancellationToken);
        }

        protected abstract void OnChange(string groupName, ChangeNotification change);

        bool IDbContext.AutoDetectChangesEnabled
        {
            get
            {
                return Configuration.AutoDetectChangesEnabled;
            }
            set
            {
                Configuration.AutoDetectChangesEnabled = value;
            }
        }

        bool IDbContext.HasChanges()
        {
            return ChangeTracker.HasChanges();
        }

        void IDbContext.DetectChanges()
        {
            ChangeTracker.DetectChanges();
        }

        IEnumerable<DbEntityEntry> IDbContext.ChangedEntries()
        {
            return ChangeTracker.Entries();
        }

        int IDbContext.SaveChanges()
        {
            return base.SaveChanges();
        }

        Task<int> IDbContext.SaveChangesAsync(CancellationToken cancellationToken)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        EntityKey IDbContext.GetEntityKey(object entity)
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;
            var objectStateEntry = objectContext.ObjectStateManager.GetObjectStateEntry(entity);
            var entityKey = objectStateEntry != null ? objectStateEntry.EntityKey : null;
            
            return entityKey;
        }

        EntityKey IDbContext.GetEntityKey<TEntity>(params object[] keyValues)
        {
            var objectSet = _objectContext.CreateObjectSet<TEntity>();
            var keyNames = objectSet.EntitySet.ElementType.KeyMembers.Select(k => k.Name);

            return new EntityKey(objectSet.EntitySet.Name, keyNames.Zip(keyValues, (k, v) => new EntityKeyMember(k, v)));
        }

        EntityType IDbContext.GetEntityModelMetadata(Type entityType)
        {
            return _objectContext.GetEntityModelMetadata(entityType);
        }

        DbEntityEntry IDbContext.Entry(object entity)
        {
            return Entry(entity);
        }
    }
}
