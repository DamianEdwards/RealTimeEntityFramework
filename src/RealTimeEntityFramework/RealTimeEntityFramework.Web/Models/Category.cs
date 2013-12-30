using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RealTimeEntityFramework.Web.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<Post> Posts { get; set;  }
    }
}