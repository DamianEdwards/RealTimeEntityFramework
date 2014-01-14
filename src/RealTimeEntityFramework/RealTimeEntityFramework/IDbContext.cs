using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
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
    }
}
