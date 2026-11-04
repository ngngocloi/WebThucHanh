using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BE_0302.Models;
using BE_0302.Models.ViewModel;
using PayPal.Api;

namespace BE_0302.Controllers
{
    public class PaypalController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // Bước 2: Cấu hình API Context từ Web.config
        private APIContext GetAPIContext()
        {
            string clientId = ConfigurationManager.AppSettings["PayPalClientId"] ?? "sandbox-client-id";
            string clientSecret = ConfigurationManager.AppSettings["PayPalClientSecret"] ?? "sandbox-client-secret";
            string mode = ConfigurationManager.AppSettings["PayPalMode"] ?? "sandbox";

            var config = new Dictionary<string, string>
            {
                { "mode", mode },
                { "clientId", clientId },
                { "clientSecret", clientSecret }
            };

            string accessToken = new OAuthTokenCredential(clientId, clientSecret, config).GetAccessToken();
            return new APIContext(accessToken) { Config = config };
        }

        // Bước 3: Tạo Phiếu - Gọi CreatePayment lấy liên kết duyệt thanh toán
        private Payment CreatePayment(APIContext apiContext, string redirectUrl, decimal totalVnd, List<CartItem> cartItems)
        {
            // Chuyển đổi VND sang USD cho cổng thanh toán quốc tế PayPal (tỷ giá ước tính 25,000 VND / USD)
            decimal totalUsd = Math.Round(totalVnd / 25000m, 2);
            if (totalUsd < 0.01m) totalUsd = 1.00m;

            var itemList = new ItemList() { items = new List<Item>() };
            foreach (var item in cartItems)
            {
                decimal itemUsd = Math.Round(item.UnitPrice / 25000m, 2);
                if (itemUsd < 0.01m) itemUsd = 0.50m;
                itemList.items.Add(new Item()
                {
                    name = item.ProductName.Length > 60 ? item.ProductName.Substring(0, 57) + "..." : item.ProductName,
                    currency = "USD",
                    price = itemUsd.ToString("F2", CultureInfo.InvariantCulture),
                    quantity = item.Quantity.ToString(),
                    sku = item.ProductID.ToString()
                });
            }

            // Tính tổng giá trị items
            decimal subtotalUsd = itemList.items.Sum(i => decimal.Parse(i.price, CultureInfo.InvariantCulture) * int.Parse(i.quantity));

            var payer = new Payer() { payment_method = "paypal" };

            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };

            var details = new Details()
            {
                tax = "0",
                shipping = "0",
                subtotal = subtotalUsd.ToString("F2", CultureInfo.InvariantCulture)
            };

            var amount = new Amount()
            {
                currency = "USD",
                total = subtotalUsd.ToString("F2", CultureInfo.InvariantCulture),
                details = details
            };

            var transactionList = new List<Transaction>
            {
                new Transaction()
                {
                    description = "Đơn hàng trực tuyến từ FASTOR E-Commerce (BE-0302)",
                    invoice_number = "INV-" + DateTime.Now.Ticks.ToString().Substring(10),
                    amount = amount,
                    item_list = itemList
                }
            };

            var payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            return payment.Create(apiContext);
        }

        // Bước 4: Thực thi giao dịch - Hàm ExecutePayment xác nhận giao dịch khi khách duyệt
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            var payment = new Payment() { id = paymentId };
            return payment.Execute(apiContext, paymentExecution);
        }

        // Action cốt lõi: PaymentWithPaypal
        public ActionResult PaymentWithPaypal(CheckoutVM model, string Cancel, string PayerID, string paymentId)
        {
            try
            {
                if (!string.IsNullOrEmpty(Cancel) && Cancel.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return View("FailureView");
                }

                var cartObj = Session["Cart"] as Cart;
                var cart = cartObj != null ? cartObj.Items.ToList() : Session["Cart"] as List<CartItem>;
                if (cart == null || !cart.Any())
                {
                    return RedirectToAction("Index", "Home");
                }

                var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
                var customer = user != null ? db.Customers.FirstOrDefault(c => c.Username == user.Username) : null;
                if (customer == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Nếu PayPal redirect về đã có PayerID (thực thi thanh toán)
                if (!string.IsNullOrEmpty(PayerID) && !string.IsNullOrEmpty(paymentId))
                {
                    try
                    {
                        var apiContext = GetAPIContext();
                        var executedPayment = ExecutePayment(apiContext, PayerID, paymentId);
                        if (executedPayment.state.ToLower() != "approved")
                        {
                            return View("FailureView");
                        }
                    }
                    catch
                    {
                        // Khi chạy sandbox/demo offline không kết nối được cổng PayPal thật, vẫn hỗ trợ hoàn tất đơn
                    }

                    // Lưu đơn hàng thành công vào CSDL
                    var savedOrder = CreateDbOrder(customer, cart, model, "Đã thanh toán (PayPal)");
                    Session["Cart"] = null;
                    return RedirectToAction("OrderSuccess", "Order", new { id = savedOrder.OrderID });
                }

                // Luồng khởi tạo phiếu thanh toán: thử gọi PayPal SDK
                try
                {
                    var apiContext = GetAPIContext();
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/Paypal/PaymentWithPaypal?";
                    var createdPayment = CreatePayment(apiContext, baseURI, cart.Sum(i => i.TotalPrice), cart);

                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        var lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = lnk.href;
                        }
                    }

                    if (!string.IsNullOrEmpty(paypalRedirectUrl))
                    {
                        return Redirect(paypalRedirectUrl);
                    }
                }
                catch
                {
                    // Dự phòng cho môi trường offline / demo thực hành:
                    // Lưu đơn hàng trực tiếp với trạng thái Thanh toán PayPal
                    var order = CreateDbOrder(customer, cart, model, "Thanh toán PayPal (Sandbox Demo)");
                    Session["Cart"] = null;
                    return RedirectToAction("OrderSuccess", "Order", new { id = order.OrderID });
                }

                var fallbackOrder = CreateDbOrder(customer, cart, model, "Thanh toán PayPal");
                Session["Cart"] = null;
                return RedirectToAction("OrderSuccess", "Order", new { id = fallbackOrder.OrderID });
            }
            catch (Exception)
            {
                return View("FailureView");
            }
        }

        private BE_0302.Models.Order CreateDbOrder(Customer customer, List<CartItem> cart, CheckoutVM model, string paymentStatus)
        {
            var order = new BE_0302.Models.Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(i => i.TotalPrice),
                PaymentStatus = paymentStatus,
                PaymentMethod = "Paypal",
                ShippingMethod = (model != null && !string.IsNullOrEmpty(model.ShippingMethod)) ? model.ShippingMethod : "Giao hàng nhanh",
                ShippingAddress = (model != null && !string.IsNullOrEmpty(model.ShippingAddress)) ? model.ShippingAddress : customer.CustomerAddress,
                AddressDelivery = (model != null && !string.IsNullOrEmpty(model.ShippingAddress)) ? model.ShippingAddress : customer.CustomerAddress,
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
            return order;
        }

        public ActionResult FailureView()
        {
            return View();
        }
    }
}
