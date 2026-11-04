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
        private MyStoreEntities db = new MyStoreEntities();

        // Hàm lấy dịch vụ giỏ hàng
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        private void PopulateSimilarProducts(Cart cart, int? page)
        {
            var products = db.Products.ToList();
            var similarProducts = new List<Product>();

            if (cart.Items != null && cart.Items.Any())
            {
                similarProducts = products.Where(p => p.Category != null
                    && cart.Items.Any(ci => ci.Category == p.Category.CategoryName)
                    && !cart.Items.Any(ci => ci.ProductID == p.ProductID)).ToList();
            }

            int pageNumber = page ?? 1;
            int pageSize = cart.PageSize;
            cart.SimilarProducts = similarProducts.OrderBy(p => p.ProductID).ToPagedList(pageNumber, pageSize);
        }

        // Hiển thị giỏ hàng không gom nhóm theo danh mục
        public ActionResult Index(int? page)
        {
            var cart = GetCartService().GetCart();
            PopulateSimilarProducts(cart, page);
            return View(cart);
        }

        // Hiển thị giỏ hàng đã gom nhóm sản phẩm theo danh mục (Giỏ hàng nâng cao)
        public ActionResult Index2(int? page)
        {
            var cart = GetCartService().GetCart();
            PopulateSimilarProducts(cart, page);
            return View(cart);
        }

        // Thêm sản phẩm vào giỏ
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var product = db.Products.Find(id);
            if (product != null)
            {
                var cartService = GetCartService();
                string categoryName = product.Category != null ? product.Category.CategoryName : "Khác";
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

        // Alias hỗ trợ tên Action AddItem trong slide tổng quan
        public ActionResult AddItem(int id, int quantity = 1)
        {
            return AddToCart(id, quantity);
        }

        // Xóa sản phẩm khỏi giỏ
        public ActionResult RemoveFromCart(int id)
        {
            var cartService = GetCartService();
            cartService.GetCart().RemoveItem(id);
            return RedirectToAction("Index");
        }

        // Alias hỗ trợ tên Action RemoveItem
        public ActionResult RemoveItem(int id)
        {
            return RemoveFromCart(id);
        }

        // Làm trống giỏ hàng
        public ActionResult ClearCart()
        {
            GetCartService().ClearCart();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            var cartService = GetCartService();
            cartService.GetCart().UpdateQuantity(id, quantity);
            return RedirectToAction("Index");
        }
    }
}
