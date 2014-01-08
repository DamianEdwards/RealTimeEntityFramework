using System.Collections.Generic;

namespace RealTimeEntityFramework.Web.Models.ViewModels
{
    public class PostsIndexViewModel
    {
        public int CategoryId { get; set; }

        public IEnumerable<Post> Posts { get; set; }
    }
}