using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeEntityFramework
{
    public class DbContextChangeNotifier
    {
        private readonly IDbContext _dbContext;

        internal DbContextChangeNotifier(IDbContext dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException("dbContext");
            }

            _dbContext = dbContext;
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

        public string GetPrimaryKeyNotificationGroupName<TEntity>(params object[] keyValues) where TEntity : class
        {
            return GetPrimaryKeyNotificationGroupName(typeof(TEntity).FullName, _dbContext.GetEntityKey<TEntity>(keyValues));
        }

        private static string GetPrimaryKeyNotificationGroupName(string entityTypeName, EntityKey entityKey)
        {
            if (entityKey != null && entityKey.EntityKeyValues != null)
            {
                // Build group name for entity key, format: [EntityTypeName]_PrimaryKey_[Key1Name]_[Key1Value]_[Key2Name]_[Key2Value]
                var prefix = String.Format("{0}_PrimaryKey", entityTypeName);
                return entityKey.EntityKeyValues.Aggregate(prefix, (name, k) => String.Format("{0}_{1}_{2}", name, k.Key, k.Value));
            }

            return null;
        }

        public IEnumerable<string> GetPredicateNotificationGroupNames<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            // TODO: Support predicates with multiple compatible conditions, which result in multiple groups, e.g. p => p.CategoryId = categoryId && p.IsVisible

            var valueExtractor = new PredicateExpressionValuesExtractor<TEntity>(_dbContext);
            valueExtractor.Visit(predicate);

            if (!valueExtractor.IsValid)
            {
                throw new InvalidOperationException("The predicate provided does not support change notifications.");
            }

            // Build group name for predicate value, format: [EntityTypeName]_Set_[FieldName]_[FieldValue]
            var groupNamePrefix = String.Format("{0}_Set", typeof(TEntity).FullName);
            foreach (var predicateField in valueExtractor.PredicateFields)
            {
                var groupName = String.Format("{0}_{1}_{2}", groupNamePrefix, predicateField.Key, predicateField.Value);

                yield return groupName;
            }
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

                changes.Add(changeDetails);
            }

            return changes;
        }

        private void Notify(List<ChangeDetails> changes)
        {
            // Inspect the changes and push update notifications to generated groups
            foreach (var change in changes)
            {
                // Notify subscribers listening for changes to this specific entity
                NotifyPrimaryKeyGroup(change);

                // Notify subscribers listening for changes to sets which contain/contained this entity
                NotifyPrimaryKeyPredicateGroups(change);
                NotifyForeignKeyPredicateGroups(change);
                NotifyAnnotatedPropertyPredicateGroups(change);
            }
        }

        private void NotifyPrimaryKeyGroup(ChangeDetails change)
        {
            if (change.EntityState != EntityState.Added)
            {
                var groupName = GetPrimaryKeyNotificationGroupName(change.Entity.GetType().FullName, change.EntityKey);
                var notification = ChangeNotification.Create(
                    ChangeScope.SpecificEntity,
                    change.EntityState,
                    change);
                OnChange(groupName, notification);
            }
        }

        private void NotifyPrimaryKeyPredicateGroups(ChangeDetails change)
        {

        }

        private void NotifyForeignKeyPredicateGroups(ChangeDetails change)
        {
            // Foreign keys, [EntityTypeName]_Set_[ForeignKeyName]_[KeyValue]
            var foreignKeyGroupNamePrefix = String.Format("{0}_Set", change.Entity.GetType().FullName);

            var entityMetadata = _dbContext.GetEntityModelMetadata(change.Entity.GetType());

            // TODO: Support multi-property foreign keys
            var foreignKeys = entityMetadata.NavigationProperties
                .Select(np => np.GetDependentProperties().FirstOrDefault())
                .Where(p => p != null);

            string groupName;
            ChangeNotification notification;

            foreach (var key in foreignKeys)
            {
                var keyProperty = _dbContext.Entry(change.Entity).Property(key.Name);

                if (keyProperty.ParentProperty != null)
                {
                    // This is a complex property so umm, yeah, no idea what we should do here :S
                    continue;
                }

                if (change.EntityState == EntityState.Added ||
                    change.EntityState == EntityState.Modified && change.PropertyChanges.Any(c => c.PropertyName == key.Name))
                {
                    // Notify the group for the current set this entity was added to
                    groupName = String.Format("{0}_{1}_{2}", foreignKeyGroupNamePrefix, keyProperty.Name, keyProperty.CurrentValue);
                    notification = ChangeNotification.Create(
                        ChangeScope.EntitySet,
                        ChangeType.Added,
                        change,
                        key.Name);
                    OnChange(groupName, notification);

                    if (change.EntityState == EntityState.Modified)
                    {
                        // Notify the group for the original set this entity was removed from
                        groupName = String.Format("{0}_{1}_{2}", foreignKeyGroupNamePrefix, keyProperty.Name, keyProperty.OriginalValue);
                        notification = ChangeNotification.Create(
                            ChangeScope.EntitySet,
                            ChangeType.Deleted,
                            change,
                            key.Name);
                        OnChange(groupName, notification);
                    }
                }
                else
                {
                    // The key didn't change or the entity was deleted so just tell the current set group that an entity changed
                    groupName = String.Format("{0}_{1}_{2}", foreignKeyGroupNamePrefix, keyProperty.Name, keyProperty.CurrentValue);
                    notification = ChangeNotification.Create(
                        ChangeScope.EntitySet,
                        ChangeType.Updated,
                        change,
                        key.Name);
                    OnChange(groupName, notification);
                }
            }
        }

        private void NotifyAnnotatedPropertyPredicateGroups(ChangeDetails change)
        {
            // TODO: Support notifications for changes to explicitly annotated primitive properties
        }

        private static void ValidateDbContextType(Type dbContextType)
        {
            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
            {
                throw new ArgumentException("The type provided for parameter 'dbContextType' must derive from System.Data.Entity.DbContext.", "dbContextType");
            }
        }
    }
}
