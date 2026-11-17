using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BE_0302.Helpers;
using BE_0302.Models.ViewModel;
using PagedList;
namespace BE_0302.Areas.Admin.Controllers {
    public class OrdersController : AdminController {
        public ActionResult Index(int? customerID, string keyword, string paymentStatus, DateTime? orderDate, string sortOrder, int? page) {
            keyword = (keyword ?? "").Trim();
            if (keyword.Length > 200) keyword = keyword.Substring(0, 200);
            var model = new OrderSearchVM { Keyword = keyword, CustomerID = customerID, PaymentStatus = paymentStatus, OrderDate = orderDate, SortOrder = sortOrder };
            var orders = db.Orders.Include(o => o.Customer).AsQueryable();
            if (customerID.HasValue) {
                var c = db.Customers.Find(customerID.Value);
                if (c == null) return HttpNotFound();
                model.CustomerName = c.CustomerName;
                orders = orders.Where(o => o.CustomerID == customerID.Value);
            }
            if (keyword.Length > 0) orders = orders.Where(o => o.Customer.CustomerName.Contains(keyword) || o.Customer.Username.Contains(keyword));
            if (!string.IsNullOrEmpty(paymentStatus)) {
                if (!OrderWorkflow.IsPayment(paymentStatus)) ModelState.AddModelError("PaymentStatus", "Trạng thái không hợp lệ.");
                else orders = orders.Where(o => o.PaymentStatus == paymentStatus);
            }
            if (orderDate.HasValue) {
                var date = orderDate.Value.Date;
                orders = orders.Where(o => DbFunctions.TruncateTime(o.OrderDate) == date);
            }
            if (!ModelState.IsValid) orders = orders.Where(o => false);
            var count = orders.Count();
            switch (sortOrder) {
                case "date_asc": orders = orders.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderID); break;
                case "customer": orders = orders.OrderBy(o => o.Customer.CustomerName).ThenByDescending(o => o.OrderID); break;
                case "total_asc": orders = orders.OrderBy(o => o.TotalAmount).ThenByDescending(o => o.OrderID); break;
                case "total_desc": orders = orders.OrderByDescending(o => o.TotalAmount).ThenByDescending(o => o.OrderID); break;
                default: model.SortOrder = "date_desc"; orders = orders.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID); break;
            }
            model.Orders = orders.ToPagedList(PageHelper.Clamp(page, count, 15), 15);
            return View(model);
        }
        private BE_0302.Models.Order Find(int id) {
            return db.Orders.Include(o => o.Customer).Include(o => o.OrderDetails.Select(d => d.Product)).SingleOrDefault(o => o.OrderID == id);
        }
        public ActionResult DetailedOrder(int? id) {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var order = Find(id.Value);
            return order == null ? (ActionResult)HttpNotFound() : View("DetailedOrder", order);
        }
        public ActionResult Details(int? id) { return DetailedOrder(id); }
        public ActionResult UpdateStatus(int? id) {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var order = Find(id.Value);
            if (order == null) return HttpNotFound();
            return View(new OrderStatusVM { Order = order, OrderID = order.OrderID, OrderStatus = order.OrderStatus, PaymentStatus = order.PaymentStatus, Version = Convert.ToBase64String(order.RowVersion) });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(OrderStatusVM model) {
            var order = Find(model.OrderID);
            if (order == null) return HttpNotFound();
            byte[] version = null;
            try { version = Convert.FromBase64String(model.Version ?? ""); } catch (FormatException) { }
            if (version == null || version.Length != 8) ModelState.AddModelError("", "Phiên bản đơn hàng không hợp lệ. Vui lòng tải lại trang.");
            if (!OrderWorkflow.CanChange(order.OrderStatus, model.OrderStatus)) ModelState.AddModelError("OrderStatus", "Không thể chuyển sang trạng thái đã chọn.");
            if (!OrderWorkflow.IsPayment(model.PaymentStatus)) ModelState.AddModelError("PaymentStatus", "Trạng thái thanh toán không hợp lệ.");
            model.Order = order;
            if (!ModelState.IsValid) return View(model);
            db.Entry(order).Property(o => o.RowVersion).OriginalValue = version;
            order.OrderStatus = model.OrderStatus; order.PaymentStatus = model.PaymentStatus;
            db.Entry(order).Property(o => o.OrderStatus).IsModified = true;
            try {
                db.SaveChanges(); TempData["Success"] = "Đã cập nhật trạng thái đơn hàng.";
                return RedirectToAction("DetailedOrder", new { id = order.OrderID });
            } catch (DbUpdateConcurrencyException) {
                if (db.Entry(order).GetDatabaseValues() == null) return HttpNotFound();
                db.Entry(order).Reload();
                model.OrderStatus = order.OrderStatus; model.PaymentStatus = order.PaymentStatus;
                model.Version = Convert.ToBase64String(order.RowVersion); ModelState.Clear();
                ModelState.AddModelError("", "Đơn hàng vừa thay đổi. Kiểm tra trạng thái mới rồi lưu lại.");
                return View(model);
            }
        }
    }
}
