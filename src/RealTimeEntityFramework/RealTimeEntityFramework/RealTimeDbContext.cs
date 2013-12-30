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
            // Capture changes before the save as it will reset the change tracker
            var changes = CaptureChanges();

            var result = base.SaveChanges();
            
            // Notify the subscribers
            NotifySubscribers(changes);
            
            return result;
        }

        /// <summary>
        /// Adds a callback to be invoked when changes are saved by the context.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="callback">The callback to be invoked.</param>
        /// <returns>An object that when disposed cancels the subscription.</returns>
        public static IDisposable Subscribe<TEntity>(Action<ChangeDetails<TEntity>> callback)
        {
            var subscription = new Subscription<TEntity>(callback, s => Unsubscribe(typeof(TEntity), s));

            try
            {
                _locker.EnterWriteLock();

                List<ISubscription> entityTypeSubscriptions;
                if (!_subscriptions.TryGetValue(typeof(TEntity), out entityTypeSubscriptions))
                {
                    entityTypeSubscriptions = new List<ISubscription>();
                    _subscriptions.Add(typeof(TEntity), entityTypeSubscriptions);
                }
                entityTypeSubscriptions.Add(subscription);
            }
            finally
            {
                _locker.ExitWriteLock();
            }

            return subscription;
        }

        private Dictionary<Type, List<Tuple<ChangeType, object>>> CaptureChanges()
        {
            // TODO: Should we honor AutoDetectChanges setting here and throw if false?
            ChangeTracker.DetectChanges();

            var changes = new Dictionary<Type, List<Tuple<ChangeType, object>>>();
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
                {
                    continue;
                }

                var entityType = entry.Entity.GetType();

                List<Tuple<ChangeType, object>> typeChanges;
                if (!changes.TryGetValue(entityType, out typeChanges))
                {
                    typeChanges = new List<Tuple<ChangeType, object>>();
                    changes.Add(entityType, typeChanges);
                }

                ChangeType changeType;
                switch (entry.State)
                {
                    case EntityState.Added:
                        changeType = ChangeType.Insert;
                        break;
                    case EntityState.Deleted:
                        changeType = ChangeType.Delete;
                        break;
                    case EntityState.Modified:
                    default:
                        changeType = ChangeType.Update;
                        break;
                }

                typeChanges.Add(new Tuple<ChangeType, object>(changeType, entry.Entity));
            }
            return changes;
        }

        private static void NotifySubscribers(Dictionary<Type, List<Tuple<ChangeType, object>>> changes)
        {
            try
            {
                _locker.EnterReadLock();

                foreach (var kvp in changes)
                {
                    var entityType = kvp.Key;
                    var entityTypeChanges = kvp.Value;

                    var entityTypeSubscriptions = _subscriptions[entityType];
                    if (entityTypeSubscriptions != null)
                    {
                        foreach (var subscription in entityTypeSubscriptions)
                        {
                            foreach (var change in entityTypeChanges)
                            {
                                subscription.Notify(change.Item2, change.Item1);
                            }
                        }
                    }
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }


        private static void Unsubscribe(Type entityType, ISubscription subscription)
        {
            try
            {
                _locker.EnterWriteLock();

                List<ISubscription> entityTypeSubscriptions;
                if (_subscriptions.TryGetValue(entityType, out entityTypeSubscriptions))
                {
                    entityTypeSubscriptions.Remove(subscription);
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }
}
