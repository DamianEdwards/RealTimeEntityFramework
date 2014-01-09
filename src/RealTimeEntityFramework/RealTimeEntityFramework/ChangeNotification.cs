using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RealTimeEntityFramework
{
    public struct ChangeNotification
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeScope Scope { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeType ChangeType { get; set; }

        public IEnumerable<string> SourceFieldNames { get; set; }

        public Type EntityType { get; set; }

        public Dictionary<string, object> EntityKeys { get; set; }

        internal static ChangeNotification Create(ChangeScope scope, EntityState entityState, ChangeDetails change, params string[] sourceFieldNames)
        {
            return Create(scope, ChangeTypeFromEntityState(entityState), change, sourceFieldNames);
        }

        internal static ChangeNotification Create(ChangeScope scope, ChangeType changeType, ChangeDetails change, params string[] sourceFieldNames)
        {
            return new ChangeNotification
            {
                Scope = scope,
                ChangeType = ChangeType.Added,
                SourceFieldNames = sourceFieldNames,
                EntityType = change.Entity.GetType(),
                EntityKeys = change.EntityKey.EntityKeyValues.ToDictionary(k => k.Key, k => k.Value)
            };
        }

        private static ChangeType ChangeTypeFromEntityState(EntityState entityState)
        {
            switch (entityState)
            {
                case EntityState.Added:
                    return ChangeType.Added;
                case EntityState.Deleted:
                    return ChangeType.Deleted;
                case EntityState.Detached:
                    break;
                case EntityState.Modified:
                    return ChangeType.Updated;
                case EntityState.Unchanged:
                    break;
                default:
                    break;
            }

            throw new ArgumentException("entityState");
        }
    }

    public enum ChangeScope
    {
        SpecificEntity,
        EntitySet
    }

    public enum ChangeType
    {
        Added,
        Updated,
        Deleted
    }
}
