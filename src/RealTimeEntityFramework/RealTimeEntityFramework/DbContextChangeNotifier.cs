using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    internal class DbContextChangeNotifier
    {
        private readonly IDbContext _dbContext;
        private readonly EntityNotificationGroupManager _notificationGroupManager;

        internal DbContextChangeNotifier(IDbContext dbContext, EntityNotificationGroupManager notificationGroupManager)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            _dbContext = dbContext;
            _notificationGroupManager = notificationGroupManager;

            OnChange += (_, __) => { };
        }

        public event Action<string, ChangeNotification> OnChange;

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
            Notify(changes);

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
            SetEntityKeysOnAddedEntities(changes);

            // Notify the subscribers
            Notify(changes);

            if (resetChangeTracking)
            {
                _dbContext.AutoDetectChangesEnabled = true;
            }

            return result;
        }

        private void SetEntityKeysOnAddedEntities(List<ChangeDetails> changes)
        {
            var insertedEntities = changes.Where(change => change.EntityState == EntityState.Added);
            
            foreach (var change in insertedEntities)
            {
                change.EntityKey = _dbContext.GetEntityKey(change.Entity);
            }
        }

        private List<ChangeDetails> CaptureChanges()
        {
            var changes = new List<ChangeDetails>();

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

                var entityKey = _dbContext.GetEntityKey(entry.Entity);
                var changeDetails = new ChangeDetails(entry.State, entityKey, entry.Entity);

                // Capture the property details
                IEnumerable<PropertyDetails> properties = null;

                if (entry.State == EntityState.Modified)
                {
                    properties = entry.OriginalValues.PropertyNames
                        .Zip(entry.CurrentValues.PropertyNames, (c, o) =>
                            new PropertyDetails(c, entry.OriginalValues[c], entry.CurrentValues[c]));
                }
                else if (entry.State == EntityState.Added)
                {
                    properties = entry.CurrentValues.PropertyNames
                        .Select(p => new PropertyDetails(p, null, entry.CurrentValues[p]));
                }
                else if (entry.State == EntityState.Deleted)
                {
                    properties = entry.OriginalValues.PropertyNames
                        .Select(p => new PropertyDetails(p, null, entry.OriginalValues[p]));
                }

                foreach (var p in properties)
                {
                    changeDetails.Properties.Add(p);
                }
                
                changes.Add(changeDetails);
            }

            return changes;
        }

        private void Notify(List<ChangeDetails> changes)
        {
            // Inspect the changes and push update notifications to generated groups
            foreach (var change in changes)
            {
                var notificationGroups = _notificationGroupManager.GetGroupsForEntity(change.Entity.GetType());

                foreach (var group in notificationGroups)
                {
                    var groupProperties = change.Properties.Where(c => group.PropertyNames.Contains(c.PropertyName));

                    if (change.EntityState == EntityState.Added ||
                        change.EntityState == EntityState.Modified && groupProperties.Any(p => p.IsChanged))
                    {
                        // Notify the group for the current set this entity was added to
                        var groupName = group.GetGroupName(groupProperties.Select(p => p.CurrentValue));
                        var notification = ChangeNotification.Create(
                            ChangeType.Added,
                            change);

                        Debug.WriteLine("Change notification: A {0} with key {1} was added to notification group {2}", change.Entity.GetType().Name, String.Join(",", change.EntityKey.EntityKeyValues.Select(k => k.Value)), groupName);

                        OnChange(groupName, notification);

                        if (change.EntityState == EntityState.Modified)
                        {
                            // Notify the group for the original set this entity was removed from
                            groupName = group.GetGroupName(groupProperties.Select(p => p.OriginalValue));
                            notification = ChangeNotification.Create(
                                ChangeType.Deleted,
                                change);

                            Debug.WriteLine("Change notification: A {0} with key {1} was removed from notification group {2}", change.Entity.GetType().Name, String.Join(",", change.EntityKey.EntityKeyValues.Select(k => k.Value)), groupName);

                            OnChange(groupName, notification);
                        }
                    }
                    else
                    {
                        // No notication group properties changed or the entity was deleted so just tell the current set group that an entity changed
                        var groupName = group.GetGroupName(groupProperties.Select(p => p.CurrentValue));
                        var notification = ChangeNotification.Create(
                            change.EntityState,
                            change);

                        Debug.WriteLine("Change notification: A {0} with key {1} was changed in notification group {2}", change.Entity.GetType().Name, String.Join(",", change.EntityKey.EntityKeyValues.Select(k => k.Value)), groupName);

                        OnChange(groupName, notification);
                    }
                }
            }
        }
    }
}
