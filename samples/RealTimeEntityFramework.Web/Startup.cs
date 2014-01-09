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
            app.MapSignalR();
        }
    }
}
