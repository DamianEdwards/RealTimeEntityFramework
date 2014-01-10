using System;
using System.Collections.Generic;
using System.Linq;

namespace RealTimeEntityFramework
{
    public class NotificationGroup
    {
        public Type EntityType { get; set; }

        public string[] PropertyNames { get; set; }

        public string GetGroupName(IEnumerable<object> values)
        {
            if (values.Count() != PropertyNames.Length)
            {
                throw new ArgumentException("values");
            }

            // Group name format: EN_Full.Type.Name_Property1Name,Property1Value_Property2Name,Property2Value

            var namePrefix = "EN_" + EntityType.FullName + "_";

            return PropertyNames.Zip(values, (name, value) => new KeyValuePair<string, object>(name, value))
                                .Aggregate(namePrefix, (name, p) => name + "_" + p.Key + "," + p.Value.ToString());
        }

        public override bool Equals(object obj)
        {
            var other = obj as NotificationGroup;

            if (other == null)
            {
                return false;
            }

            if (PropertyNames == null && other.PropertyNames == null)
            {
                return true;
            }

            return PropertyNames.SequenceEqual(other.PropertyNames, StringComparer.Ordinal);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
