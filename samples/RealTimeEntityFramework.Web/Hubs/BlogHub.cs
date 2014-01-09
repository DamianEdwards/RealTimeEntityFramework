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
                db.StartNotifications(Context, db.Posts, p => p.CategoryId == categoryId);
            }
        }

        public void StopNotificationsForPosts(int categoryId)
        {
            using (var db = new BlogDbContext())
            {
                db.StopNotifications(Context, db.Posts, p => p.CategoryId == categoryId);
            }
        }

        public Post GetPost(int id)
        {
            using (var db = new BlogDbContext())
            {
                return db.FindWithNotifications(Context, db.Posts, id);
            }
        }

        public IEnumerable<Post> GetPostsForCategory(int categoryId)
        {
            using (var db = new BlogDbContext())
            {
                return db.SelectWithNotifications(Context, db.Posts, p => p.CategoryId == categoryId)
                         .ToList();
            }
        }

        public IEnumerable<Post> GetPostsForDefaultCategory()
        {
            using (var db = new BlogDbContext())
            {
                return db.SelectWithNotifications(Context, db.Posts, p => p.CategoryId == 1)
                         .ToList();
            }
        }

        public IEnumerable<Post> GetPostsForMonth(int month)
        {
            using (var db = new BlogDbContext())
            {
                return db.SelectWithNotifications(Context, db.Posts, p => p.PublishOn.Month == month)
                         .ToList();
            }
        }
    }
}