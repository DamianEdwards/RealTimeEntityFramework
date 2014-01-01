using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    internal interface IDbContext
    {
        Type DbContextType { get; }

        bool AutoDetectChangesEnabled { get; set; }

        bool HasChanges();

        void DetectChanges();

        IEnumerable<DbEntityEntry> ChangedEntries();

        Func<int> SaveChanges { get; }

        Func<CancellationToken, Task<int>> SaveChangesAsync { get; }

        EntityKey GetEntityKey(object entity);
    }
}
