using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RealTimeEntityFramework
{
    internal struct ChangeNotification
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeScope Scope { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeType ChangeType { get; set; }

        public IEnumerable<string> SourceFieldNames { get; set; }

        public Type EntityType { get; set; }

        public IEnumerable<string> EntityKeyNames { get; set; }

        public IEnumerable<object> EntityKeyValues { get; set; }

        public static ChangeType ChangeTypeFromEntityState(EntityState entityState)
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

    internal enum ChangeScope
    {
        SpecificEntity,
        EntitySet
    }

    internal enum ChangeType
    {
        Added,
        Updated,
        Deleted
    }
}
