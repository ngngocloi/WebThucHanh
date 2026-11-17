using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BE_0302.Models;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Controllers
{
    public class CartController : Controller
    {
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
        private MyStoreEntities db = new MyStoreEntities();

        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        private void PopulateSimilarProducts(Cart cart, int? page)
        {
            var cartIds = cart.Items.Select(i => i.ProductID).Distinct().ToList();
            var categoryIds = db.Products.Where(p => cartIds.Contains(p.ProductID)).Select(p => p.CategoryID).Distinct();
            var similarProducts = db.Products.Where(p => categoryIds.Contains(p.CategoryID) && !cartIds.Contains(p.ProductID));
            int pageNumber = BE_0302.Helpers.PageHelper.Clamp(page, similarProducts.Count(), cart.PageSize);
            int pageSize = cart.PageSize;
            cart.SimilarProducts = similarProducts.OrderBy(p => p.ProductID).ToPagedList(pageNumber, pageSize);
        }

        public ActionResult Index(int? page)
        {
            var cart = GetCartService().GetCart();
            PopulateSimilarProducts(cart, page);
            return View(cart);
        }

        public ActionResult Index2(int? page) { return RedirectToAction("Index", new { page }); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Checkout(int[] selectedIds)
        {
            var cart = GetCartService().GetCart(); cart.SelectItems(selectedIds);
            if (!cart.Items.Any(i => i.Selected)) { TempData["Error"] = "Vui lòng chọn sản phẩm muốn mua."; return RedirectToAction("Index"); }
            return RedirectToAction("Checkout", "Order");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var product = db.Products.Find(id);
            if (product != null)
            {
                var cartService = GetCartService();
                string categoryName = product.Category != null ? product.Category.CategoryName : "Khác";
                if (quantity < 1 || quantity > 99 || cartService.GetCart().Items.Any(i => i.ProductID == id && i.Quantity > 99 - quantity)) {
                    TempData["Error"] = "Số lượng mỗi sản phẩm phải từ 1 đến 99.";
                    return RedirectToAction("Index");
                }
                cartService.GetCart().AddItem(
                    product.ProductID,
                    product.ProductImage,
                    product.ProductName,
                    product.ProductPrice,
                    quantity,
                    categoryName);
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddItem(int id, int quantity = 1)
        {
            return AddToCart(id, quantity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int id)
        {
            var cartService = GetCartService();
            cartService.GetCart().RemoveItem(id);
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int id)
        {
            return RemoveFromCart(id);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            GetCartService().ClearCart();
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            var cartService = GetCartService();
            if (quantity < 1 || quantity > 99) { TempData["Error"] = "Số lượng phải từ 1 đến 99."; return RedirectToAction("Index"); }
            cartService.GetCart().UpdateQuantity(id, quantity);
            return RedirectToAction("Index");
        }
    }
}
