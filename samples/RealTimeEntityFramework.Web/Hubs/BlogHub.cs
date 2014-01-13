using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using RealTimeEntityFramework.Web.Models;

namespace RealTimeEntityFramework.Web.Hubs
{
    public class BlogHub : Hub
    {
        public void StartNotificationsForPosts(int categoryId)
        {
            using (var db = new BlogDbContext())
            {
                db.StartNotifications(Context, db.Posts, new { CategoryId = categoryId });
            }
        }

        public void StartNotificationsForPosts(int categoryId, bool isVisible)
        {
            using (var db = new BlogDbContext())
            {
                db.StartNotifications(Context, db.Posts, new { CategoryId = categoryId, IsVisible = isVisible });
            }
        }

        // Ideas for property map parameter definition
        //db.StartNotifications(Context, db.Posts, new Dictionary<string, object> { { "CategoryId", categoryId } });
        //db.StartNotifications(Context, db.Posts, new { CategoryId = categoryId, IsVisible = isVisible });
        //db.StartNotifications(Context, db.Posts, p => p.CategoryId == categoryId, p => p.IsVisible);
        //db.StartNotifications(Context, db.Posts, p => p.CategoryId == categoryId && p.IsVisible);

        public void StopNotificationsForPosts(int categoryId)
        {
            using (var db = new BlogDbContext())
            {
                db.StopNotifications(Context, db.Posts, new { CategoryId = categoryId });
            }
        }

        public void StopNotificationsForPosts(int categoryId, bool isVisible)
        {
            using (var db = new BlogDbContext())
            {
                db.StopNotifications(Context, db.Posts, new { CategoryId = categoryId, IsVisible = isVisible });
            }
        }
    }
}