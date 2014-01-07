using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    internal class DbContextChangeNotifier
    {
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private static readonly Dictionary<Type, List<ISubscription>> _subscriptions = new Dictionary<Type, List<ISubscription>>();

        private readonly IDbContext _dbContext;

        public DbContextChangeNotifier(IDbContext dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            _dbContext = dbContext;
        }

        public static IDisposable Subscribe(Type dbContextType, Action<IEnumerable<ChangeDetails>> callback)
        {
            ValidateDbContextType(dbContextType);

            var subscription = new Subscription(callback, s => Unsubscribe(dbContextType, s));

            try
            {
                _locker.EnterWriteLock();

                List<ISubscription> contextSubscriptions;

                if (!_subscriptions.TryGetValue(dbContextType, out contextSubscriptions))
                {
                    contextSubscriptions = new List<ISubscription>();
                    _subscriptions.Add(dbContextType, contextSubscriptions);
                }

                contextSubscriptions.Add(subscription);
            }
            finally
            {
                _locker.ExitWriteLock();
            }

            return subscription;
        }

        private static void ValidateDbContextType(Type dbContextType)
        {
            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
            {
                throw new ArgumentException("The type provided for parameter 'dbContextType' must derive from System.Data.Entity.DbContext.", "dbContextType");
            }
        }

        public int OnSaveChanges()
        {
            var resetChangeTracking = false;

            if (_dbContext.AutoDetectChangesEnabled)
            {
                // Manually detect changes now so we can capture them before calling SaveChanges
                // as that call will reset the change tracker.
                _dbContext.DetectChanges();

                // Turn change tracking off for now so it doesn't run again during the SaveChanges
                // call below. We'll turn it on again before we return.
                _dbContext.AutoDetectChangesEnabled = false;
                resetChangeTracking = true;
            }

            var changes = CaptureChanges();

            var result = _dbContext.SaveChanges();

            // Notify the subscribers
            NotifySubscribers(_dbContext.DbContextType, changes);

            if (resetChangeTracking)
            {
                _dbContext.AutoDetectChangesEnabled = true;
            }

            return result;
        }

        public async Task<int> OnSaveChangesAsync(CancellationToken cancellationToken)
        {
            var resetChangeTracking = false;

            if (_dbContext.AutoDetectChangesEnabled)
            {
                // Manually detect changes now so we can capture them before calling SaveChanges
                // as that call will reset the change tracker.
                _dbContext.DetectChanges();

                // Turn change tracking off for now so it doesn't run again during the SaveChanges
                // call below. We'll turn it on again before we return.
                _dbContext.AutoDetectChangesEnabled = false;
                resetChangeTracking = true;
            }

            var changes = CaptureChanges();

            var result = await _dbContext.SaveChangesAsync(cancellationToken);

            // Get key information for any added entities
            UpdateEntityKeys(changes);

            // Notify the subscribers
            NotifySubscribers(_dbContext.DbContextType, changes);

            if (resetChangeTracking)
            {
                _dbContext.AutoDetectChangesEnabled = true;
            }

            return result;
        }

        private void UpdateEntityKeys(Dictionary<Type, List<ChangeDetails>> changes)
        {
            var insertedEntities = changes.Values
                .SelectMany(l => l)
                .Where(change => change.EntityState == EntityState.Added);
            
            foreach (var change in insertedEntities)
            {
                change.EntityKey = _dbContext.GetEntityKey(change.Entity);
            }
        }

        private Dictionary<Type, List<ChangeDetails>> CaptureChanges()
        {
            var changes = new Dictionary<Type, List<ChangeDetails>>();

            if (!_dbContext.HasChanges())
            {
                return changes;
            }

            var entries = _dbContext.ChangedEntries();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
                {
                    continue;
                }

                var entityType = entry.Entity.GetType();

                List<ChangeDetails> typeChanges;
                if (!changes.TryGetValue(entityType, out typeChanges))
                {
                    typeChanges = new List<ChangeDetails>();
                    changes.Add(entityType, typeChanges);
                }

                var entityKey = _dbContext.GetEntityKey(entry.Entity);
                var changeDetails = new ChangeDetails(entry.State, entityKey, entry.Entity);

                if (entry.State == EntityState.Modified)
                {
                    // Capture the actual property changes
                    var changedProperties = entry.OriginalValues.PropertyNames
                        .Zip(entry.CurrentValues.PropertyNames, (c, o) => new PropertyChange(c, entry.OriginalValues[c], entry.CurrentValues[c]))
                        .Where(c => !Object.Equals(c.CurrentValue, c.OriginalValue));

                    foreach (var p in changedProperties)
                    {
                        changeDetails.PropertyChanges.Add(p);
                    }
                }

                typeChanges.Add(changeDetails);
            }
            return changes;
        }

        private static void NotifySubscribers(Type dbContextType, Dictionary<Type, List<ChangeDetails>> changes)
        {
            ValidateDbContextType(dbContextType);

            try
            {
                _locker.EnterReadLock();

                List<ISubscription> contextSubscriptions;

                if (!_subscriptions.TryGetValue(dbContextType, out contextSubscriptions))
                {
                    return;
                }

                foreach (var kvp in changes)
                {
                    var entityType = kvp.Key;
                    var entityTypeChanges = kvp.Value;

                    foreach (var subscription in contextSubscriptions)
                    {
                        subscription.Notify(entityTypeChanges);
                    }
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        private static void Unsubscribe(Type dbContextType, ISubscription subscription)
        {
            ValidateDbContextType(dbContextType);

            try
            {
                _locker.EnterWriteLock();

                List<ISubscription> contextSubscriptions;

                if (!_subscriptions.TryGetValue(dbContextType, out contextSubscriptions))
                {
                    return;
                }

                contextSubscriptions.Remove(subscription);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }
}
