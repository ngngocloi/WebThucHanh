using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BE_0302.Models;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Controllers
{
    public class HomeController : Controller
    {
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
        private MyStoreEntities db = new MyStoreEntities();

        public ActionResult Index(string searchTerm, int? page, int? categoryId)
        {
            var model = new HomeProductVM();
            var products = db.Products.AsQueryable();

            ViewBag.Categories = db.Categories.ToList();
            ViewBag.CategoryID = categoryId;

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
                var selectedCat = db.Categories.Find(categoryId.Value);
                ViewBag.CategoryName = selectedCat != null ? selectedCat.CategoryName : null;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                               p.ProductDescription.Contains(searchTerm) ||
                                               p.Category.CategoryName.Contains(searchTerm));
            }

            int pageNumber = BE_0302.Helpers.PageHelper.Clamp(page, products.Count(), 6);
            int pageSize = 6;

            model.FeaturedProducts = products.OrderByDescending(p => p.OrderDetails.Where(d => d.Order.OrderStatus != "Cancelled").Sum(d => (int?)d.Quantity) ?? 0).ThenBy(p => p.ProductID).Take(10).ToList();

            pageNumber = BE_0302.Helpers.PageHelper.Clamp(page, Math.Min(20, products.Count()), 6);
            model.NewProducts = products.OrderBy(p => p.OrderDetails.Where(d => d.Order.OrderStatus != "Cancelled").Sum(d => (int?)d.Quantity) ?? 0).ThenBy(p => p.ProductID).Take(20).ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        public ActionResult ProductList(int? id, string searchTerm, int? page, string sortOrder)
        {
            var model = new HomeProductVM();
            var products = db.Products.AsQueryable();

            ViewBag.Categories = db.Categories.ToList();

            if (id.HasValue)
            {
                products = products.Where(p => p.CategoryID == id.Value);
                var category = db.Categories.Find(id.Value);
                ViewBag.CategoryName = category != null ? category.CategoryName : "Danh mục sản phẩm";
                ViewBag.CategoryID = id.Value;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                               p.ProductDescription.Contains(searchTerm) ||
                                               p.Category.CategoryName.Contains(searchTerm));
            }

            int pageNumber = BE_0302.Helpers.PageHelper.Clamp(page, products.Count(), 6);
            int pageSize = 6;

            model.FeaturedProducts = products.OrderByDescending(p => p.OrderDetails.Where(d => d.Order.OrderStatus != "Cancelled").Sum(d => (int?)d.Quantity) ?? 0).ThenBy(p => p.ProductID).Take(10).ToList();
            model.SortOrder = sortOrder;
            var sorted = products.OrderByDescending(p => p.ProductID);
            switch (sortOrder) {
                case "name": sorted = products.OrderBy(p => p.ProductName).ThenBy(p => p.ProductID); break;
                case "name_desc": sorted = products.OrderByDescending(p => p.ProductName).ThenBy(p => p.ProductID); break;
                case "price": sorted = products.OrderBy(p => p.ProductPrice).ThenBy(p => p.ProductID); break;
                case "price_desc": sorted = products.OrderByDescending(p => p.ProductPrice).ThenBy(p => p.ProductID); break;
            }
            model.NewProducts = sorted.ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        public ActionResult ProductDetails(int? id, int? quantity, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product pro = db.Products.Find(id);
            if (pro == null)
            {
                return HttpNotFound();
            }

            var products = db.Products.Where(p => p.CategoryID == pro.CategoryID && p.ProductID != pro.ProductID).AsQueryable();

            ProductDetailsVM model = new ProductDetailsVM();

            int pageNumber = BE_0302.Helpers.PageHelper.Clamp(page, Math.Min(8, products.Count()), 3);
            int pageSize = model.PageSize;
            model.product = pro;
            model.RelatedProducts = products.OrderBy(p => p.ProductID).Take(8).ToPagedList(1, 8);
            model.TopProducts = products.OrderByDescending(p => p.OrderDetails.Where(d => d.Order.OrderStatus != "Cancelled").Sum(d => (int?)d.Quantity) ?? 0).ThenBy(p => p.ProductID).Take(8).ToPagedList(pageNumber, pageSize);

            if (quantity.HasValue)
            {
                model.quantity = Math.Max(1, Math.Min(quantity.Value, 99));
            }
            model.estimatedValue = model.quantity * pro.ProductPrice;

            return View("ProductDetails", model);
        }

        public ActionResult ProductDetail(int? id, int? quantity, int? page)
        {
            return ProductDetails(id, quantity, page);
        }

        public ActionResult CategoryMenu()
        {
            var categories = db.Categories.ToList();
            return PartialView("_CategoriesPV", categories);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Trang giới thiệu hệ thống thương mại điện tử FASTOR (BE-0302).";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Thông tin liên hệ hỗ trợ khách hàng.";
            return View();
        }
    }
}
