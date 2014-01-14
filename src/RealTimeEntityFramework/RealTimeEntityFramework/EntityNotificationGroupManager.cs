using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RealTimeEntityFramework
{
    public class EntityNotificationGroupManager
    {
        private static ConcurrentDictionary<Type, IEnumerable<NotificationGroup>> _entityGroupsCache = new ConcurrentDictionary<Type, IEnumerable<NotificationGroup>>();

        public string GetGroupNameForEntityProperties<TEntity>(IDictionary<string, object> properties)
        {
            var group = GetGroupsForEntity<TEntity>()
                .FirstOrDefault(g => g.PropertyNames.SequenceEqual(properties.Keys.OrderBy(n => n)));

            if (group == null)
            {
                throw new ArgumentException(String.Format("No matching notifcation group for entity type {0} with the specified properties could be found. Ensure the group is defined on the type using the NotificationGroupAttribute.", typeof(TEntity).FullName), "properties");
            }

            return group.GetGroupName(properties.Values);
        }

        internal IEnumerable<NotificationGroup> GetGroupsForEntity<TEntity>()
        {
            return GetGroupsForEntity(typeof(TEntity));
        }

        internal IEnumerable<NotificationGroup> GetGroupsForEntity(Type entityType)
        {
            var entityGroups = _entityGroupsCache.GetOrAdd(entityType, t =>
            {
                var newEntityGroups = new List<NotificationGroup>();
                
                // Discover the groups for TEntity
                var groupsFromType = TypeDescriptor.GetAttributes(entityType).OfType<NotificationGroupAttribute>()
                        .Select(d => new NotificationGroup { EntityType = t, PropertyNames = d.PropertyNames.OrderBy(name => name).ToArray() });

                var groupsFromProps = TypeDescriptor.GetProperties(entityType).Cast<PropertyDescriptor>()
                    .SelectMany(p => p.Attributes.OfType<NotificationGroupAttribute>()
                                      .Select(d => new NotificationGroup
                                      {
                                          EntityType = t,
                                          PropertyNames = new[] { p.Name }
                                      }));

                var uniqueGroups = groupsFromType.Concat(groupsFromProps).Distinct();

                // TODO: Validate the properties of the groups:
                //         - have to exist on the entity model
                //         - have to be GUID, numeric, boolean or string (support DateTime possibly?)

                return uniqueGroups;
            });

            return entityGroups;
        }
    }
}
