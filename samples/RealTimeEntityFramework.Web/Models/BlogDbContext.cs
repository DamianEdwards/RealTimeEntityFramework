using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using RealTimeEntityFramework.SignalR;
using RealTimeEntityFramework.Web.Hubs;

namespace RealTimeEntityFramework.Web.Models
{
    public class BlogDbContext : HubDbContext<BlogHub>
    {
        public BlogDbContext() : base()
        {
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Post> Posts { get; set; }

        public DbSet<Comment> Comments { get; set; }
    }
}