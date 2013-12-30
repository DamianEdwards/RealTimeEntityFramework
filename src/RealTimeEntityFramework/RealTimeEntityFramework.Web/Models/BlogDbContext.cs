using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace RealTimeEntityFramework.Web.Models
{
    public class BlogDbContext : RealTimeDbContext
    {
        public BlogDbContext()
        {
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Post> Posts { get; set; }

        public DbSet<Comment> Comments { get; set; }
    }
}