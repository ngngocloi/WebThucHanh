using System.Linq;
using System.Net;
using System.Web.Mvc;
using BE_0302.Models;

namespace BE_0302.Areas.Admin.Controllers
{
    public class CategoriesController : AdminController
    {
        public ActionResult Index() { return View(db.Categories.OrderBy(c => c.CategoryName).ToList()); }
        public ActionResult Details(int? id) { return CategoryView(id); }
        public ActionResult Edit(int? id) { return CategoryView(id); }
        public ActionResult Delete(int? id) { return CategoryView(id); }
        private ActionResult CategoryView(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var category = db.Categories.Find(id.Value);
            if (category == null) return HttpNotFound();
            return View(category);
        }
        public ActionResult Create() { return View(new Category()); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                category.CategoryName = category.CategoryName.Trim();
                db.Categories.Add(category);
                if (SaveChanges()) return RedirectToAction("Index");
            }
            return View(category);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            var existing = db.Categories.Find(category.CategoryID);
            if (existing == null) return HttpNotFound();
            if (ModelState.IsValid)
            {
                existing.CategoryName = category.CategoryName.Trim();
                if (SaveChanges()) return RedirectToAction("Index");
            }
            return View(category);
        }
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();
            if (db.Products.Any(p => p.CategoryID == id))
                ModelState.AddModelError("", "Danh mục còn sản phẩm. Hãy chuyển hoặc xóa các sản phẩm trước.");
            else
            {
                db.Categories.Remove(category);
                if (SaveChanges()) return RedirectToAction("Index");
            }
            return View(category);
        }
    }
}
