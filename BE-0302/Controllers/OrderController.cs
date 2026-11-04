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
            var cartObj = Session["Cart"] as Cart;
            if (cartObj != null && cartObj.Items != null)
            {
                return cartObj.Items.ToList();
            }
            return Session["Cart"] as List<CartItem>;
        }

        // GET: Order
        public ActionResult Index()
        {
            return RedirectToAction("MyOrder");
        }

        // GET: Order/Checkout
        [Authorize]
        public ActionResult Checkout()
        {
            // Kiểm tra giỏ hàng trong session,
            // nếu giỏ hàng rỗng hoặc không có sản phẩm thì chuyển hướng về trang chủ
            var cart = GetCartItemsFromSession();
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // Xác thực người dùng đã đăng nhập chưa, nếu chưa thì chuyển hướng tới trang Đăng nhập
            var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin khách hàng từ CSDL, nếu chưa có thì chuyển hướng tới trang Đăng nhập
            // nếu có rồi thì lấy địa chỉ của khách hàng và gán vào ShippingAddress của CheckoutVM
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

        // POST: Order/Checkout
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
            if (model.OrderDate == default(DateTime))
            {
                model.OrderDate = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                // Nếu người dùng chọn thanh toán bằng Paypal, sẽ điều hướng tới trang PaymentWithPaypal
                if (model.PaymentMethod == "Paypal")
                {
                    return RedirectToAction("PaymentWithPaypal", "Paypal", model);
                }

                // Thiết lập PaymentStatus dựa trên PaymentMethod
                string paymentStatus = "Chưa thanh toán";
                switch (model.PaymentMethod)
                {
                    case "Tiền mặt": paymentStatus = "Thanh toán tiền mặt"; break;
                    case "Paypal": paymentStatus = "Thanh toán paypal"; break;
                    case "Mua trước trả sau": paymentStatus = "Trả góp"; break;
                    default: paymentStatus = "Chưa thanh toán"; break;
                }

                // Tạo đơn hàng và chi tiết đơn hàng liên quan
                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = model.OrderDate,
                    TotalAmount = model.TotalAmount,
                    PaymentStatus = paymentStatus,
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

                // Lưu đơn hàng vào CSDL
                db.Orders.Add(order);
                db.SaveChanges();

                // Xóa giỏ hàng sau khi đặt hàng thành công
                Session["Cart"] = null;

                // Điều hướng tới trang Xác nhận đơn hàng
                return RedirectToAction("OrderSuccess", new { id = order.OrderID });
            }

            return View(model);
        }

        // GET: Order/OrderSuccess/5
        public ActionResult OrderSuccess(int id)
        {
            var order = db.Orders.Include("OrderDetails").SingleOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Order/MyOrder
        [Authorize]
        public ActionResult MyOrder(string searchTerm, string status)
        {
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

            var ordersQuery = db.Orders
                .Include("OrderDetails.Product")
                .Where(o => o.CustomerID == customer.CustomerID)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "Tất cả đơn")
            {
                ordersQuery = ordersQuery.Where(o => o.PaymentStatus.Contains(status));
            }

            var orders = ordersQuery.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID).ToList();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                orders = orders.Where(o =>
                    o.OrderID.ToString().Contains(searchTerm) ||
                    o.OrderDetails.Any(od => od.Product != null && od.Product.ProductName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentStatus = string.IsNullOrEmpty(status) ? "Tất cả đơn" : status;

            return View("MyOrder", orders);
        }

        // Alias hỗ trợ link Order/OrderHistory trong _Layout.cshtml của slide
        [Authorize]
        public ActionResult OrderHistory(string searchTerm, string status)
        {
            return MyOrder(searchTerm, status);
        }
    }
}
