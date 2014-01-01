using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using RealTimeEntityFramework.Web.Models;

[assembly: OwinStartup(typeof(RealTimeEntityFramework.Web.Startup))]

namespace RealTimeEntityFramework.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            NotifyingDbContext.Subscribe(typeof(BlogDbContext), details =>
            {
                Debug.WriteLine("Entity change notification received: A {0} was {1} ", details.Entity.GetType().Name, details.EntityState);
            });

            app.MapSignalR();
        }
    }
}
