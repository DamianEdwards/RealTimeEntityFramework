using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    internal interface IDbContext
    {
        bool AutoDetectChangesEnabled { get; set; }

        bool HasChanges();

        void DetectChanges();

        IEnumerable<DbEntityEntry> ChangedEntries();

        int SaveChanges();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

        EntityKey GetEntityKey(object entity);

        EntityKey GetEntityKey<TEntity>(params object[] keyValues) where TEntity : class;

        EntityType GetEntityModelMetadata(Type entityType);

        DbEntityEntry Entry(object entity);
    }
}
