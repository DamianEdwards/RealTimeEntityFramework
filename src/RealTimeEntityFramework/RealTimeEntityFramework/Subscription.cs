using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealTimeEntityFramework
{
    internal interface ISubscription
    {
        void Notify(object entity, ChangeType changeType);
    }

    internal class Subscription<TEntity> : ISubscription, IDisposable
    {
        private readonly Action<ChangeDetails<TEntity>> _onChange;
        private readonly Action<ISubscription> _dispose;
   
        public Subscription(Action<ChangeDetails<TEntity>> onChange, Action<ISubscription> dispose)
        {
            _onChange = onChange ?? (_ => { });
            _dispose = dispose ?? (_ => { });
        }

        public void Notify(object entity, ChangeType changeType)
        {
            var details = new ChangeDetails<TEntity>(changeType, (TEntity) entity);
            _onChange(details);
        }

        public void Dispose()
        {
            _dispose(this);
        }
    }
}
