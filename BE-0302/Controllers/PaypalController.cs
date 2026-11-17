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
    [Authorize]
    public class PaypalController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

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

        private Payment CreatePayment(APIContext apiContext, string redirectUrl, decimal totalVnd, List<CartItem> cartItems)
        {



            var itemList = new ItemList() { items = new List<Item>() };
            foreach (var item in cartItems)
            {
                decimal itemUsd = Math.Round(item.UnitPrice / VndPerUsd, 2);
                if (itemUsd < 0.01m) throw new InvalidOperationException("Gia tri san pham nho hon muc thanh toan toi thieu.");
                itemList.items.Add(new Item()
                {
                    name = item.ProductName.Length > 60 ? item.ProductName.Substring(0, 57) + "..." : item.ProductName,
                    currency = "USD",
                    price = itemUsd.ToString("F2", CultureInfo.InvariantCulture),
                    quantity = item.Quantity.ToString(),
                    sku = item.ProductID.ToString()
                });
            }

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

        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            var payment = new Payment() { id = paymentId };
            return payment.Execute(apiContext, paymentExecution);
        }

        public ActionResult PaymentWithPaypal(string Cancel, string PayerID, string paymentId, string state)
        {
            var model = Session["PayPalCheckout"] as CheckoutVM;
            var cart = Session["PayPalCart"] as List<CartItem>;
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return RedirectToAction("Login", "Account");
            if (model == null || cart == null || !cart.Any() || model.CustomerID != customer.CustomerID)
                return RedirectToAction("Checkout", "Order");
            if (!IsConfigured()) return Failure("PayPal hiện chưa sẵn sàng. Vui lòng chọn thanh toán khi nhận hàng.");
            try
            {
                if (!string.IsNullOrEmpty(Cancel)) {
                    if (state != Session["PayPalState"] as string) return new HttpStatusCodeResult(400);
                    ClearPending();
                    return Failure("Bạn đã hủy thanh toán. Các sản phẩm vẫn còn trong giỏ hàng.");
                }
                if (!string.IsNullOrEmpty(paymentId) || !string.IsNullOrEmpty(PayerID)) {
                    if (paymentId != Session["PayPalPaymentId"] as string || string.IsNullOrWhiteSpace(PayerID) || state != Session["PayPalState"] as string)
                        return new HttpStatusCodeResult(400);
                    var api = GetAPIContext();
                    var payment = Payment.Get(api, paymentId);
                    if (payment.state != "approved") payment = ExecutePayment(api, PayerID, paymentId);
                    decimal total;
                    var expected = (decimal)Session["PayPalExpectedUsd"];
                    bool verified = payment.id == paymentId && payment.state == "approved" &&
                        payment.transactions != null && payment.transactions.Count == 1 &&
                        payment.transactions[0].amount.currency == "USD" &&
                        decimal.TryParse(payment.transactions[0].amount.total, NumberStyles.Number, CultureInfo.InvariantCulture, out total) && total == expected &&
                        payment.transactions[0].related_resources != null &&
                        payment.transactions[0].related_resources.Any(x => x.sale != null && x.sale.state == "completed");
                    if (!verified) return Failure("PayPal chưa xác nhận thanh toán hoàn tất. Đơn hàng chưa được tạo.");
                    var order = new BE_0302.Models.Order {
                        CustomerID = customer.CustomerID, OrderDate = DateTime.UtcNow.AddHours(7),
                        TotalAmount = cart.Sum(i => i.TotalPrice), PaymentStatus = "Paid", OrderStatus = "Processing",
                        PaymentMethod = "Paypal", DeliveryMethod = model.ShippingMethod, ShippingAddress = model.ShippingAddress,
                        OrderDetails = cart.Select(i => new OrderDetail { ProductID = i.ProductID, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList() };
                    db.Orders.Add(order); db.SaveChanges();
                    new CartService(Session).GetCart().RemovePurchased(cart); ClearPending();
                    return RedirectToAction("OrderSuccess", "Order", new { id = order.OrderID });
                }
                var token = Guid.NewGuid().ToString("N");
                Session["PayPalState"] = token;
                string callback = Url.Action("PaymentWithPaypal", "Paypal", new { state = token }, Request.Url.Scheme);
                var created = CreatePayment(GetAPIContext(), callback, cart.Sum(i => i.TotalPrice), cart);
                var approval = created.links.FirstOrDefault(l => l.rel == "approval_url");
                if (approval == null) return Failure("Không thể mở trang thanh toán PayPal. Vui lòng thử lại.");
                Session["PayPalPaymentId"] = created.id;
                Session["PayPalExpectedUsd"] = decimal.Parse(created.transactions[0].amount.total, CultureInfo.InvariantCulture);
                return Redirect(approval.href);
            }
            catch (Exception error)
            {
                System.Diagnostics.Trace.TraceWarning("PayPal checkout failed: {0}", error.GetType().Name);
                return Failure("Không thể xác nhận thanh toán PayPal. Giỏ hàng được giữ lại để bạn kiểm tra và thử lại.");
            }
        }
        public static decimal VndPerUsd {
            get {
                decimal rate;
                return decimal.TryParse(ConfigurationManager.AppSettings["PayPalVndPerUsd"], NumberStyles.Number,
                    CultureInfo.InvariantCulture, out rate) && rate > 0 ? rate : 0;
            }
        }
        public static bool IsConfigured()
        {
            var id = ConfigurationManager.AppSettings["PayPalClientId"];
            var secret = ConfigurationManager.AppSettings["PayPalClientSecret"];
            return VndPerUsd > 0 && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(secret) && !id.StartsWith("YOUR_") && !secret.StartsWith("YOUR_");
        }
        private void ClearPending()
        {
            foreach (string key in new[] { "PayPalCheckout", "PayPalCart", "PayPalState", "PayPalPaymentId", "PayPalExpectedUsd" }) Session.Remove(key);
        }
        private ActionResult Failure(string message) { ViewBag.Message = message; return View("FailureView"); }
        public ActionResult FailureView() { return Failure("Thanh toán chưa hoàn tất. Vui lòng kiểm tra và thử lại."); }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
