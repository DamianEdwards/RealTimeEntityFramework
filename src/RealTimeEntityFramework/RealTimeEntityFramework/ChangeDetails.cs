using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace RealTimeEntityFramework
{
    public class ChangeDetails
    {
        public ChangeDetails(EntityState entityState, object entity)
        {
            if (entityState == System.Data.Entity.EntityState.Detached
                || entityState == System.Data.Entity.EntityState.Unchanged)
            {
                throw new ArgumentException("entityState");
            }

            EntityState = entityState;
            Entity = entity;
        }

        public virtual object Entity { get; private set; }
        public virtual EntityState EntityState { get; private set; }
    }
}
