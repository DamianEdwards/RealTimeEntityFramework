using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using RealTimeEntityFramework.Web.Models;

namespace RealTimeEntityFramework.Web.Hubs
{
    public class BlogHub : Hub
    {
        public Post GetPost(int id)
        {
            using (var db = new BlogDbContext())
            {
                return db.Find(Context, db.Posts, id);
            }
        }

        public IEnumerable<Post> GetPostsForCategory(int categoryId)
        {
            using (var db = new BlogDbContext())
            {
                return db.Select(Context, db.Posts, p => p.CategoryId == categoryId);
            }
        }

        public IEnumerable<Post> GetPostsForDefaultCategory()
        {
            using (var db = new BlogDbContext())
            {
                return db.Select(Context, db.Posts, p => p.CategoryId == 1);
            }
        }

        public IEnumerable<Post> GetPostsForMonth(int month)
        {
            using (var db = new BlogDbContext())
            {
                return db.Select(Context, db.Posts, p => p.PublishOn.Month == month);
            }
        }
    }
}