using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealTimeEntityFramework
{
    public class ChangeDetails<TEntity>
    {
        public ChangeDetails(ChangeType changeType, TEntity entity)
        {
            ChangeType = changeType;
            Entity = entity;
        }

        public virtual TEntity Entity { get; private set; }
        public virtual ChangeType ChangeType { get; private set; }
    }

    public enum ChangeType
    {
        Insert,
        Update,
        Delete
    }
}
