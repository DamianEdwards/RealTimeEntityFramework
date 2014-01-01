using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;

namespace RealTimeEntityFramework
{
    internal interface ISubscription
    {
        void Notify(EntityState entityState, EntityKey entityKey, object entity);
    }

    internal class Subscription : ISubscription, IDisposable
    {
        private readonly Action<ChangeDetails> _onChange;
        private readonly Action<ISubscription> _dispose;
   
        public Subscription(Action<ChangeDetails> onChange, Action<ISubscription> dispose)
        {
            _onChange = onChange ?? (_ => { });
            _dispose = dispose ?? (_ => { });
        }

        public void Notify(EntityState entityState, EntityKey entityKey, object entity)
        {
            var details = new ChangeDetails(entityState, entityKey, entity);
            _onChange(details);
        }

        public void Dispose()
        {
            _dispose(this);
        }
    }
}
