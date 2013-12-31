using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    public abstract class RealTimeDbContext : DbContext
    {
        private DbContextChangeNotifier _changeNotifier;

        public RealTimeDbContext()
            : base()
        {
            Initialize();
        }

        public RealTimeDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Initialize();
        }

        public RealTimeDbContext(DbCompiledModel model)
            : base(model)
        {
            Initialize();
        }

        public RealTimeDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            Initialize();
        }

        public RealTimeDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Initialize();
        }

        public RealTimeDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            Initialize();
        }

        public RealTimeDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            Initialize();
        }

        private void Initialize()
        {
            _changeNotifier = new DbContextChangeNotifier(new DbContextAdapter(this, () => base.SaveChanges(), ct => base.SaveChangesAsync(ct)));
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
        public static IDisposable Subscribe(Type dbContextType, Action<ChangeDetails> callback)
        {
            return DbContextChangeNotifier.Subscribe(dbContextType, callback);
        }
    }
}
