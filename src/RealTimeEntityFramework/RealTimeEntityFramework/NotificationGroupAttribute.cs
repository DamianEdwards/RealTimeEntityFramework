using System;
using System.Collections.Generic;

namespace RealTimeEntityFramework
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class NotificationGroupAttribute : Attribute
    {
        private readonly string[] _propertyNames;

        public NotificationGroupAttribute()
        {
            _propertyNames = new string[0];
        }

        public NotificationGroupAttribute(params string[] propertyNames)
        {
            _propertyNames = propertyNames;
        }

        public IEnumerable<string> PropertyNames
        {
            get { return _propertyNames; }
        }
    }
}
