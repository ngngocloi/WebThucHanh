using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BE_0302.Helpers;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Areas.Admin.Controllers
{
    public class OrdersController : AdminController
    {
        public ActionResult Index(int? customerID, string keyword, int? page)
        {
            keyword = (keyword ?? "").Trim();
            var model = new OrderSearchVM { Keyword = keyword, CustomerID = customerID };
            var orders = db.Orders.Include(o => o.Customer).AsQueryable();
            if (customerID.HasValue)
            {
                var customer = db.Customers.Find(customerID.Value);
                if (customer == null) return HttpNotFound();
                model.CustomerName = customer.CustomerName;
                orders = orders.Where(o => o.CustomerID == customerID.Value);
            }
            if (keyword.Length > 0) orders = orders.Where(o => o.Customer.CustomerName.Contains(keyword)
                || o.Customer.Username.Contains(keyword) || o.PaymentStatus.Contains(keyword));
            model.Orders = orders.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID)
                .ToPagedList(PageHelper.Clamp(page, orders.Count()), 10);
            return View(model);
        }
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var order = db.Orders.Include(o => o.Customer).Include(o => o.OrderDetails.Select(d => d.Product))
                .SingleOrDefault(o => o.OrderID == id.Value);
            if (order == null) return HttpNotFound();
            return View(order);
        }
    }
}
