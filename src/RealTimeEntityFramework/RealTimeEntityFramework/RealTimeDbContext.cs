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
        private static readonly List<ISubscription> _subscriptions = new List<ISubscription>();

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
            NotifySubscribers(changes);

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
            NotifySubscribers(changes);

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
        public static IDisposable Subscribe(Action<ChangeDetails> callback)
        {
            var subscription = new Subscription(callback, s => Unsubscribe(s));

            try
            {
                _locker.EnterWriteLock();

                _subscriptions.Add(subscription);
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

        private static void NotifySubscribers(Dictionary<Type, List<ChangeDetails>> changes)
        {
            try
            {
                _locker.EnterReadLock();

                foreach (var kvp in changes)
                {
                    var entityType = kvp.Key;
                    var entityTypeChanges = kvp.Value;

                    foreach (var subscription in _subscriptions)
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


        private static void Unsubscribe(ISubscription subscription)
        {
            try
            {
                _locker.EnterWriteLock();

                _subscriptions.Remove(subscription);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }
}
