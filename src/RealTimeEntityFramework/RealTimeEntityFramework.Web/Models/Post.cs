using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RealTimeEntityFramework.Web.Models
{
    public class Post
    {
        public int Id { get; set; }

        public string Title { get; set; }
        
        public DateTimeOffset CreatedOn { get; set; }
        
        public DateTimeOffset PublishOn { get; set; }
        
        public Category Category { get; set; }
        
        public int CategoryId { get; set; }

        public string Content { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
}