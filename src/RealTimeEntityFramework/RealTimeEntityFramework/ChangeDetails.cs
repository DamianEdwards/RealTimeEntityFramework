using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;

namespace RealTimeEntityFramework
{
    public class ChangeDetails
    {
        public ChangeDetails(EntityState entityState, EntityKey entityKey, object entity)
        {
            if (entityState == System.Data.Entity.EntityState.Detached
                || entityState == System.Data.Entity.EntityState.Unchanged)
            {
                throw new ArgumentException("entityState");
            }

            EntityState = entityState;
            EntityKey = entityKey;
            Entity = entity;
        }

        public virtual EntityState EntityState { get; private set; }

        public virtual EntityKey EntityKey { get; private set; }

        public virtual object Entity { get; private set; }
    }
}
