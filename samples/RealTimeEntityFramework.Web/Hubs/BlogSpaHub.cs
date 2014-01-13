using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace RealTimeEntityFramework.Web.Hubs
{
    public class BlogSpaHub : Hub
    {
        //public Post GetPost(int id)
        //{
        //    using (var db = new BlogDbContext())
        //    {
        //        return db.FindWithNotifications(Context, db.Posts, id);
        //    }
        //}

        //public IEnumerable<Post> GetPostsForCategory(int categoryId)
        //{
        //    using (var db = new BlogDbContext())
        //    {
        //        return db.SelectWithNotifications(Context, db.Posts, p => p.CategoryId == categoryId)
        //                 .ToList();
        //    }
        //}

        //public IEnumerable<Post> GetPostsForDefaultCategory()
        //{
        //    using (var db = new BlogDbContext())
        //    {
        //        return db.SelectWithNotifications(Context, db.Posts, p => p.CategoryId == 1)
        //                 .ToList();
        //    }
        //}

        //public IEnumerable<Post> GetPostsForMonth(int month)
        //{
        //    using (var db = new BlogDbContext())
        //    {
        //        return db.SelectWithNotifications(Context, db.Posts, p => p.PublishOn.Month == month)
        //                 .ToList();
        //    }
        //}
    }
}