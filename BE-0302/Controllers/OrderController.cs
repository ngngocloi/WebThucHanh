using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BE_0302.Models;
using BE_0302.Models.ViewModel;

namespace BE_0302.Controllers
{
    public class OrderController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        private List<CartItem> GetCartItemsFromSession()
        {
            var cart = Session["Cart"] as Cart;
            var items = cart != null ? cart.Items.Where(i => i.Selected).ToList() : Session["Cart"] as List<CartItem>;
            if (items == null || items.Count == 0 || items.Any(i => i.Quantity < 1 || i.Quantity > 99)) return null;
            var ids = items.Select(i => i.ProductID).ToList();
            var products = db.Products.Where(p => ids.Contains(p.ProductID)).ToList();
            if (products.Count != ids.Distinct().Count()) { TempData["Error"] = "Một sản phẩm không còn được bán. Vui lòng kiểm tra lại giỏ hàng."; return null; }
            return items.Select(i => {
                var product = products.Single(p => p.ProductID == i.ProductID);
                return new CartItem { ProductID = i.ProductID, ProductImage = product.ProductImage, ProductName = product.ProductName,
                    Quantity = i.Quantity, UnitPrice = product.ProductPrice, Category = i.Category };
            }).ToList();
        }

        public ActionResult Index()
        {
            return RedirectToAction("MyOrder");
        }

        [Authorize]
        public ActionResult Checkout()
        {

            var cart = GetCartItemsFromSession();
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = db.Customers.FirstOrDefault(c => c.Username == user.Username);
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new CheckoutVM
            {
                CartItems = cart,
                TotalAmount = cart.Sum(item => item.TotalPrice),
                OrderDate = DateTime.Now,
                ShippingAddress = customer.CustomerAddress,
                CustomerID = customer.CustomerID,
                Username = customer.Username
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Checkout(CheckoutVM model)
        {
            var cart = GetCartItemsFromSession();
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = db.Customers.FirstOrDefault(c => c.Username == user.Username);
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.CartItems = cart;
            model.TotalAmount = cart.Sum(item => item.TotalPrice);
            model.OrderDate = DateTime.UtcNow.AddHours(7);

            model.CustomerID = customer.CustomerID;
            model.Username = customer.Username;
            if (!new[] { "Tiền mặt", "Paypal" }.Contains(model.PaymentMethod)) ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán không hợp lệ.");
            if (!new[] { "Giao hàng nhanh", "Giao hàng tiết kiệm" }.Contains(model.ShippingMethod)) ModelState.AddModelError("ShippingMethod", "Phương thức giao hàng không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.ShippingAddress)) ModelState.AddModelError("ShippingAddress", "Vui lòng nhập địa chỉ giao hàng.");
            if (model.TotalAmount > 9999999999999999.99m) ModelState.AddModelError("", "Tổng giá trị đơn hàng vượt giới hạn cho phép.");
            if (model.PaymentMethod == "Paypal" && !PaypalController.IsConfigured())
                ModelState.AddModelError("PaymentMethod", "PayPal hiện chưa sẵn sàng. Vui lòng chọn thanh toán khi nhận hàng.");
            if (ModelState.IsValid)
            {

                if (model.PaymentMethod == "Paypal")
                {
                    Session["PayPalCheckout"] = model;
                    Session["PayPalCart"] = cart;
                    return RedirectToAction("PaymentWithPaypal", "Paypal");
                }

                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = model.OrderDate,
                    TotalAmount = model.TotalAmount,
                    PaymentStatus = "Unpaid",
                    OrderStatus = "Processing",
                    PaymentMethod = model.PaymentMethod,
                    ShippingMethod = model.ShippingMethod,
                    ShippingAddress = model.ShippingAddress,
                    AddressDelivery = model.ShippingAddress,
                    OrderDetails = cart.Select(item => new OrderDetail
                    {
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                db.Orders.Add(order);
                db.SaveChanges();

                new CartService(Session).GetCart().RemovePurchased(cart);

                return RedirectToAction("OrderSuccess", new { id = order.OrderID });
            }

            return View(model);
        }

        [Authorize]
        public ActionResult OrderSuccess(int id)
        {
            var username = User.Identity.Name;
            var order = db.Orders.Include("OrderDetails.Product").SingleOrDefault(o => o.OrderID == id && o.Customer.Username == username);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [Authorize]
        public ActionResult MyOrder(string searchTerm, string status, string more)
        {
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return RedirectToAction("Login", "Account");
            var own = db.Orders.Where(o => o.CustomerID == customer.CustomerID);
            var model = new OrderHistoryVM { SearchTerm = (searchTerm ?? "").Trim(),
                Status = BE_0302.Helpers.OrderWorkflow.IsState(status) ? status : "All",
                More = more, HasAnyOrders = own.Any(), Groups = new List<OrderGroupVM>() };
            if (model.SearchTerm.Length > 200) model.SearchTerm = model.SearchTerm.Substring(0, 200);
            var query = own.Include(o => o.OrderDetails.Select(d => d.Product));
            if (model.SearchTerm.Length > 0) {
                int id; int.TryParse(model.SearchTerm.TrimStart('#'), out id);
                string term = model.SearchTerm;
                query = query.Where(o => o.OrderID == id || o.OrderDetails.Any(d => d.Product.ProductName.Contains(term)));
            }
            foreach (string state in BE_0302.Helpers.OrderWorkflow.States.Where(v => model.Status == "All" || model.Status == v)) {
                var filtered = query.Where(o => o.OrderStatus == state);
                int count = filtered.Count();
                if (count == 0) continue;
                bool expanded = more == state;
                model.Groups.Add(new OrderGroupVM { Status = state, TotalCount = count, Expanded = expanded,
                    Orders = filtered.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID).Take(expanded ? 50 : 2).ToList() });
            }
            return View("MyOrder", model);
        }
        [Authorize]
        public ActionResult OrderHistory(string searchTerm, string status, string more) { return MyOrder(searchTerm, status, more); }
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Reorder(int id, int? productId)
        {
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return RedirectToAction("Login", "Account");
            var order = db.Orders.Include(o => o.OrderDetails.Select(d => d.Product.Category))
                .SingleOrDefault(o => o.OrderID == id && o.CustomerID == customer.CustomerID);
            if (order == null) return HttpNotFound();
            var lines = order.OrderDetails.Where(d => !productId.HasValue || d.ProductID == productId.Value)
                .GroupBy(d => d.ProductID).Select(g => g.First()).ToList();
            if (lines.Count == 0) return HttpNotFound();
            var cart = new CartService(Session).GetCart();
            int added = 0;
            foreach (var line in lines) {
                if (line.Product == null) continue;
                var item = cart.Items.FirstOrDefault(i => i.ProductID == line.ProductID);
                if (item != null && item.Quantity >= 99) continue;
                var product = line.Product;
                cart.AddItem(product.ProductID, product.ProductImage, product.ProductName, product.ProductPrice, 1, product.Category.CategoryName);
                added++;
            }
            TempData[added == 0 ? "Error" : "Success"] = added == 0 ? "Không thể mua lại: sản phẩm không còn được bán hoặc đã đạt giới hạn 99." : "Đã thêm mỗi sản phẩm mua lại với số lượng 1 vào giỏ.";
            return RedirectToAction("Index", "Cart");
        }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
