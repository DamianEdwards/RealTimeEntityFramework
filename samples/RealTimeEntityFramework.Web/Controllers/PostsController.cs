using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using RealTimeEntityFramework.Web.Models;
using RealTimeEntityFramework.Web.Models.ViewModels;

namespace RealTimeEntityFramework.Web.Controllers
{
    public class PostsController : Controller
    {
        private BlogDbContext db = new BlogDbContext();

        // GET: /Posts/
        public async Task<ActionResult> Index(int categoryId = 1, bool? isVisible = null)
        {
            var query = db.Posts.Where(p => p.CategoryId == categoryId);

            if (isVisible.HasValue)
            {
                query = query.Where(p => p.IsVisible == isVisible.Value);
            }

            var viewModel = new PostsIndexViewModel
            {
                CategoryId = categoryId,
                CategoriesList = new SelectList(db.Categories, "Id", "Name", selectedValue: 1),
                IsVisible = isVisible,
                IsVisibleList = new List<SelectListItem>
                {
                    new SelectListItem
                    {
                        Value = "",
                        Text = "All Posts"
                    },
                    new SelectListItem
                    {
                        Value = "true",
                        Text = "Visible Posts only"
                    },
                    new SelectListItem
                    {
                        Value = "false",
                        Text = "Invisible Posts only"
                    }
                },
                Posts = await query
                    .Include(p => p.Category)
                    .ToListAsync()
            };

            return View("IndexAjax", viewModel);
        }

        // GET: /Posts/IndexRows
        public async Task<ActionResult> IndexRows(int categoryId = 1, bool? isVisible = null)
        {
            var query = db.Posts.Where(p => p.CategoryId == categoryId);

            if (isVisible.HasValue)
            {
                query = query.Where(p => p.IsVisible == isVisible.Value);
            }

            return View("_IndexRows", await query
                .Include(p => p.Category)
                .ToListAsync());
        }

        // GET: /Posts/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Post post = await db.Posts.FindAsync(id);

            if (post == null)
            {
                return HttpNotFound();
            }

            return View(post);
        }

        // GET: /Posts/Create
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", selectedValue: 1);

            return View(new Post { PublishOn = DateTime.Now });
        }

        // POST: /Posts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Title,PublishOn,CategoryId,Content,IsVisible")] Post post)
        {
            post.CreatedOn = DateTimeOffset.Now;

            if (ModelState.IsValid)
            {
                db.Posts.Add(post);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        // GET: /Posts/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = await db.Posts.FindAsync(id);
            if (post == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        // POST: /Posts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id /* [Bind(Include="Id,Title,PublishOn,CategoryId,Content,IsVisible")] Post post*/)
        {
            var post = await db.Posts.FindAsync(id);

            if (TryUpdateModel(post, new[] { "Title", "PublishOn", "CategoryId", "Content", "IsVisible" })
                && ModelState.IsValid)
            {
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(db.Categories, "Id", "Name", post.CategoryId);

            return View(post);
        }

        // GET: /Posts/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Post post = await db.Posts.FindAsync(id);
            if (post == null)
            {
                return HttpNotFound();
            }
            return View(post);
        }

        // POST: /Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Post post = await db.Posts.FindAsync(id);
            db.Posts.Remove(post);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
