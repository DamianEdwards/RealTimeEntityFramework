using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    internal class DbContextAdapter : IDbContext
    {
        private readonly DbContext _dbContext;

        public DbContextAdapter(DbContext dbContext, Func<int> saveChanges, Func<CancellationToken, Task<int>> saveChangesAsync)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            _dbContext = dbContext;
            SaveChanges = saveChanges ?? (() => default(int));
            SaveChangesAsync = saveChangesAsync ?? (_ => Task.FromResult(default(int)));
        }

        public Type DbContextType
        {
            get { return _dbContext.GetType(); }
        }

        public bool AutoDetectChangesEnabled
        {
            get
            {
                return _dbContext.Configuration.AutoDetectChangesEnabled;
            }
            set
            {
                _dbContext.Configuration.AutoDetectChangesEnabled = value;
            }
        }

        public bool HasChanges()
        {
            return _dbContext.ChangeTracker.HasChanges();
        }

        public void DetectChanges()
        {
            _dbContext.ChangeTracker.DetectChanges();
        }

        public IEnumerable<DbEntityEntry> ChangedEntries()
        {
            return _dbContext.ChangeTracker.Entries();
        }

        public Func<int> SaveChanges { get; private set; }

        public Func<CancellationToken, Task<int>> SaveChangesAsync { get; private set; }


        public EntityKey GetEntityKey(object entity)
        {
            var objectContext = ((IObjectContextAdapter)_dbContext).ObjectContext;
            var objectStateEntry = objectContext.ObjectStateManager.GetObjectStateEntry(entity);
            var entityKey = objectStateEntry != null ? objectStateEntry.EntityKey : null;
            
            return entityKey;
        }
    }
}
