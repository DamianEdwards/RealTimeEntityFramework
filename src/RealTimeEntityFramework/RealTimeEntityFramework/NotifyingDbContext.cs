using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    /// <summary>
    /// A DbContext base class the sends notifications to subscribers whenever any tracked entities are added, updated or deleted.
    /// </summary>
    public abstract class NotifyingDbContext : DbContext, IDbContext
    {
        private DbContextChangeNotifier _changeNotifier;

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

        private void Initialize()
        {
            _changeNotifier = new DbContextChangeNotifier(this);
        }

        public override int SaveChanges()
        {
            return _changeNotifier.OnSaveChanges();
        }

        public override Task<int> SaveChangesAsync()
        {
            return SaveChangesAsync(CancellationToken.None);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _changeNotifier.OnSaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Adds a callback to be invoked when changes are saved by the context.
        /// </summary>
        /// <param name="callback">The callback to be invoked.</param>
        /// <returns>An object that when disposed cancels the subscription.</returns>
        public static IDisposable Subscribe(Type dbContextType, Action<IEnumerable<ChangeDetails>> callback)
        {
            return DbContextChangeNotifier.Subscribe(dbContextType, callback);
        }

        Type IDbContext.DbContextType
        {
            get { return GetType(); }
        }

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
    }
}
