using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace RealTimeEntityFramework
{
    public abstract class HubDbContext<THub> : RealTimeDbContext where THub : IHub
    {
        private readonly IDisposable _subscription;
        private readonly IHubContext _hubContext;

        public HubDbContext()
            : this(GlobalHost.ConnectionManager)
        {

        }

        public HubDbContext(IConnectionManager connectionManager)
            : base()
        {
            ClientEntityUpdatedMethodName = "entityUpdated";

            _hubContext = connectionManager.GetHubContext<THub>();

            _subscription = Subscribe(GetType(), details =>
            {
                ((IClientProxy)_hubContext.Clients.All).Invoke(ClientEntityUpdatedMethodName, details);
            });
        }

        public string ClientEntityUpdatedMethodName { get; set; }

        protected override void Dispose(bool disposing)
        {
            _subscription.Dispose();

            base.Dispose(disposing);
        }
    }
}
