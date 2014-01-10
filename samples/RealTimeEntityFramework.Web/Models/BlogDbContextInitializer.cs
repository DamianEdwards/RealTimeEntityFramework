using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace RealTimeEntityFramework.Web.Models
{
    public class BlogDbContextInitializer : DropCreateDatabaseAlways<BlogDbContext>
    {
        protected override void Seed(BlogDbContext context)
        {
            context.ChangeNotificationsEnabled = false;

            context.Categories.Add(new Category { Name = "Default" });

            context.SaveChanges();
        }
    }
}