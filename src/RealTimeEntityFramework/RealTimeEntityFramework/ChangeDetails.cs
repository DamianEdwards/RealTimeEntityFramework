using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealTimeEntityFramework
{
    public class ChangeDetails
    {
        public ChangeDetails(ChangeType changeType, object entity)
        {
            ChangeType = changeType;
            Entity = entity;
        }

        public virtual object Entity { get; private set; }
        public virtual ChangeType ChangeType { get; private set; }
    }

    public enum ChangeType
    {
        Insert,
        Update,
        Delete
    }
}
