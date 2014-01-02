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
        void Notify(IEnumerable<ChangeDetails> changeDetails);
    }

    internal class Subscription : ISubscription, IDisposable
    {
        private readonly Action<IEnumerable<ChangeDetails>> _onChange;
        private readonly Action<ISubscription> _dispose;

        public Subscription(Action<IEnumerable<ChangeDetails>> onChange, Action<ISubscription> dispose)
        {
            _onChange = onChange ?? (_ => { });
            _dispose = dispose ?? (_ => { });
        }

        public void Notify(IEnumerable<ChangeDetails> changeDetails)
        {
            _onChange(changeDetails);
        }

        public void Dispose()
        {
            _dispose(this);
        }
    }
}
