﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;

namespace RealTimeEntityFramework
{
    internal class ChangeDetails
    {
        public ChangeDetails(EntityState entityState, EntityKey entityKey, object entity)
        {
            if (entityState == EntityState.Detached || entityState == EntityState.Unchanged)
            {
                throw new ArgumentException("entityState");
            }

            EntityState = entityState;
            EntityKey = entityKey;
            Entity = entity;
            Properties = new List<PropertyDetails>();
        }

        public EntityState EntityState { get; private set; }

        public EntityKey EntityKey { get; internal set; }

        public object Entity { get; private set; }

        public IList<PropertyDetails> Properties { get; private set; }
    }

    public class PropertyDetails
    {
        public PropertyDetails(string propertyName, object originalValue, object currentValue)
        {
            PropertyName = propertyName;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }

        public string PropertyName { get; set; }
        
        public object OriginalValue { get; set; }
        
        public object CurrentValue { get; set; }

        public bool IsChanged
        {
            get { return !OriginalValue.Equals(CurrentValue); }
        }
    }
}
