using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RealTimeEntityFramework.SignalR
{
    public class SignalRChangeNotification
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeType ChangeType { get; set; }

        public IEnumerable<string> SourceFieldNames { get; set; }

        public string EntityType { get; set; }

        public Dictionary<string, object> EntityKeys { get; set; }

        internal static SignalRChangeNotification Create(ChangeNotification original)
        {
            return new SignalRChangeNotification
            {
                ChangeType = original.ChangeType,
                SourceFieldNames = original.SourceFieldNames,
                EntityType = original.EntityType.FullName,
                EntityKeys = original.EntityKeys
            };
        }
    }
}
