using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RealTimeEntityFramework.Web.Models
{
    [NotificationGroup("CategoryId", "IsVisible")]
    public class Post
    {
        [NotificationGroup]
        public int Id { get; set; }
        
        public string Title { get; set; }
        
        [Display(Name="Created On")]
        public DateTimeOffset CreatedOn { get; set; }
        
        public DateTimeOffset PublishOn { get; set; }
        
        public Category Category { get; set; }
        
        [NotificationGroup]
        public int CategoryId { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        public bool IsVisible { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
}