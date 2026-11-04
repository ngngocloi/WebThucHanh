using System;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BE_0302.Helpers;
using BE_0302.Models;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Areas.Admin.Controllers
{
    public class ProductsController : AdminController
    {
        public ActionResult Index([Bind(Include = "Keyword,MinPrice,MaxPrice,SortOrder")] ProductSearchVM model, int? page)
        {
            model.Keyword = (model.Keyword ?? "").Trim();
            var products = db.Products.Include(p => p.Category).AsQueryable();
            if (model.Keyword.Length > 0)
                products = products.Where(p => p.ProductName.Contains(model.Keyword)
                    || p.ProductDescription.Contains(model.Keyword) || p.Category.CategoryName.Contains(model.Keyword));
            if (model.MinPrice > model.MaxPrice)
                ModelState.AddModelError("", "Giá tối thiểu không được lớn hơn giá tối đa.");
            if (ModelState.IsValid)
            {
                if (model.MinPrice.HasValue) products = products.Where(p => p.ProductPrice >= model.MinPrice.Value);
                if (model.MaxPrice.HasValue) products = products.Where(p => p.ProductPrice <= model.MaxPrice.Value);
            }
            switch (model.SortOrder)
            {
                case "name_desc": products = products.OrderByDescending(p => p.ProductName).ThenBy(p => p.ProductID); break;
                case "price_asc": products = products.OrderBy(p => p.ProductPrice).ThenBy(p => p.ProductID); break;
                case "price_desc": products = products.OrderByDescending(p => p.ProductPrice).ThenBy(p => p.ProductID); break;
                default: model.SortOrder = "name_asc"; products = products.OrderBy(p => p.ProductName).ThenBy(p => p.ProductID); break;
            }
            model.PageSize = 10;
            model.PageNumber = PageHelper.Clamp(page, products.Count(), model.PageSize);
            model.Products = products.ToPagedList(model.PageNumber, model.PageSize);
            return View(model);
        }
        public ActionResult Details(int? id) { return ProductView(id); }
        public ActionResult Delete(int? id) { return ProductView(id); }
        public ActionResult Edit(int? id)
        {
            var result = ProductView(id);
            var view = result as ViewResult;
            if (view != null) LoadCategories(((Product)view.Model).CategoryID);
            return result;
        }
        private ActionResult ProductView(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var product = db.Products.Include(p => p.Category).SingleOrDefault(p => p.ProductID == id.Value);
            if (product == null) return HttpNotFound();
            return View(product);
        }
        private void LoadCategories(int? selected = null)
        {
            ViewBag.CategoryID = new SelectList(db.Categories.OrderBy(c => c.CategoryName), "CategoryID", "CategoryName", selected);
        }
        public ActionResult Create()
        {
            LoadCategories();
            return View(new Product());
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryID,ProductName,ProductDescription,ProductDecription,ProductPrice,UploadImg")] Product product)
        {
            ValidateCategory(product.CategoryID);
            product.ProductImage = "~/Content/images/default-product.svg";
            if (ModelState.IsValid && SaveImage(product))
            {
                product.ProductName = product.ProductName.Trim();
                db.Products.Add(product);
                if (SaveChanges()) return RedirectToAction("Index");
            }
            LoadCategories(product.CategoryID);
            return View(product);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,CategoryID,ProductName,ProductDescription,ProductDecription,ProductPrice,UploadImg")] Product product)
        {
            var existing = db.Products.Find(product.ProductID);
            if (existing == null) return HttpNotFound();
            product.ProductImage = existing.ProductImage;
            ValidateCategory(product.CategoryID);
            if (ModelState.IsValid && SaveImage(product))
            {
                existing.CategoryID = product.CategoryID;
                existing.ProductName = product.ProductName.Trim();
                existing.ProductDescription = product.ProductDescription;
                existing.ProductPrice = product.ProductPrice;
                existing.ProductImage = product.ProductImage;
                if (SaveChanges()) return RedirectToAction("Index");
            }
            LoadCategories(product.CategoryID);
            return View(product);
        }
        private void ValidateCategory(int id)
        {
            if (!db.Categories.Any(c => c.CategoryID == id))
                ModelState.AddModelError("CategoryID", "Danh mục không tồn tại. Vui lòng chọn lại.");
        }
        private bool SaveImage(Product product)
        {
            var file = product.UploadImg;
            if (file == null || file.ContentLength == 0) return true;
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extension) || file.ContentLength > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("UploadImg", "Chỉ chọn ảnh JPG, JPEG, PNG hoặc GIF, tối đa 5 MB.");
                return false;
            }
            try
            {
                using (var image = Image.FromStream(file.InputStream, true, true))
                {
                    var format = image.RawFormat;
                    bool valid = (extension == ".png" && format.Equals(System.Drawing.Imaging.ImageFormat.Png))
                        || (extension == ".gif" && format.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                        || ((extension == ".jpg" || extension == ".jpeg") && format.Equals(System.Drawing.Imaging.ImageFormat.Jpeg));
                    if (!valid) throw new ArgumentException();
                }
                file.InputStream.Position = 0;
                string directory = Server.MapPath("~/Content/images/");
                Directory.CreateDirectory(directory);
                string name = Guid.NewGuid().ToString("N") + extension;
                file.SaveAs(Path.Combine(directory, name));
                product.ProductImage = "~/Content/images/" + name;
                return true;
            }
            catch (Exception error) when (error is ArgumentException || error is IOException
                || error is UnauthorizedAccessException || error is OutOfMemoryException || error is System.Runtime.InteropServices.ExternalException)
            {
                ModelState.AddModelError("UploadImg", "Không đọc hoặc lưu được ảnh. Hãy chọn ảnh hợp lệ và kiểm tra quyền ghi thư mục Content/images.");
                return false;
            }
        }
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Products.Include(p => p.Category).SingleOrDefault(p => p.ProductID == id);
            if (product == null) return HttpNotFound();
            if (db.OrderDetails.Any(d => d.ProductID == id))
                ModelState.AddModelError("", "Sản phẩm đã có trong đơn hàng, không thể xóa để bảo toàn lịch sử mua hàng.");
            else
            {
                db.Products.Remove(product);
                if (SaveChanges()) return RedirectToAction("Index");
            }
            return View(product);
        }
    }
}
