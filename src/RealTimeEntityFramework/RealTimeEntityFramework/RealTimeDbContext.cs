using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    public abstract class RealTimeDbContext : DbContext
    {
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private static readonly Dictionary<Type, List<ISubscription>> _subscriptions = new Dictionary<Type, List<ISubscription>>();

        public override int SaveChanges()
        {
            var resetChangeTracking = false;

            if (Configuration.AutoDetectChangesEnabled)
            {
                // Manually detect changes now so we can capture them before calling SaveChanges
                // as that call will reset the change tracker.
                ChangeTracker.DetectChanges();

                // Turn change tracking off for now so it doesn't run again during the SaveChanges
                // call below. We'll turn it on again before we return.
                Configuration.AutoDetectChangesEnabled = false;
                resetChangeTracking = true;
            }

            var changes = CaptureChanges();

            var result = base.SaveChanges();
            
            // Notify the subscribers
            NotifySubscribers(GetType(), changes);

            if (resetChangeTracking)
            {
                Configuration.AutoDetectChangesEnabled = true;
            }

            return result;
        }

        public override Task<int> SaveChangesAsync()
        {
            return SaveChangesAsync(CancellationToken.None);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            var resetChangeTracking = false;

            if (Configuration.AutoDetectChangesEnabled)
            {
                // Manually detect changes now so we can capture them before calling SaveChanges
                // as that call will reset the change tracker.
                ChangeTracker.DetectChanges();

                // Turn change tracking off for now so it doesn't run again during the SaveChanges
                // call below. We'll turn it on again before we return.
                Configuration.AutoDetectChangesEnabled = false;
                resetChangeTracking = true;
            }

            var changes = CaptureChanges();

            var result = await base.SaveChangesAsync(cancellationToken);

            // Notify the subscribers
            NotifySubscribers(GetType(), changes);

            if (resetChangeTracking)
            {
                Configuration.AutoDetectChangesEnabled = true;
            }

            return result;
        }

        /// <summary>
        /// Adds a callback to be invoked when changes are saved by the context.
        /// </summary>
        /// <param name="callback">The callback to be invoked.</param>
        /// <returns>An object that when disposed cancels the subscription.</returns>
        public static IDisposable Subscribe(Type dbContextType, Action<ChangeDetails> callback)
        {
            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
            {
                throw new ArgumentException("The type provided for parameter 'dbContextType' must derive from System.Data.Entity.DbContext.", "dbContextType");
            }

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

        private Dictionary<Type, List<ChangeDetails>> CaptureChanges()
        {
            var changes = new Dictionary<Type, List<ChangeDetails>>();
            
            if (!ChangeTracker.HasChanges())
            {
                return changes;
            }

            var entries = ChangeTracker.Entries();
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

                typeChanges.Add(new ChangeDetails(entry.State, entry.Entity));
            }
            return changes;
        }

        private static void NotifySubscribers(Type dbContextType, Dictionary<Type, List<ChangeDetails>> changes)
        {
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
                        foreach (var change in entityTypeChanges)
                        {
                            subscription.Notify(change.EntityState, change.Entity);
                        }
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
