using System.Collections.Generic;
using System.Web.Mvc;

namespace RealTimeEntityFramework.Web.Models.ViewModels
{
    public class PostsIndexViewModel
    {
        public PostsIndexViewModel()
        {
            
        }

        public int CategoryId { get; set; }

        public SelectList CategoriesList { get; set; }

        public bool? IsVisible { get; set; }

        public IEnumerable<SelectListItem> IsVisibleList { get; set; }

        public IEnumerable<Post> Posts { get; set; }
    }
}