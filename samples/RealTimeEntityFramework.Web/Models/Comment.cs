using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RealTimeEntityFramework.Web.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public Post Post { get; set; }

        public int PostId { get; set; }

        public string AuthorName { get; set; }

        public DateTimeOffset PublishedOn { get; set; }

        public string Content { get; set; }
    }
}