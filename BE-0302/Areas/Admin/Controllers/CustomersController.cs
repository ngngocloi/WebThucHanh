using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BE_0302.Helpers;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Areas.Admin.Controllers
{
    public class CustomersController : AdminController
    {
        public ActionResult Index(string keyword, string sortOrder, int? page)
        {
            keyword = (keyword ?? "").Trim();
            var customers = db.Customers.AsQueryable();
            if (keyword.Length > 0) customers = customers.Where(c => c.CustomerName.Contains(keyword)
                || c.Username.Contains(keyword) || c.CustomerPhone.Contains(keyword) || c.CustomerEmail.Contains(keyword));
            switch (sortOrder)
            {
                case "name_asc": customers = customers.OrderBy(c => c.CustomerName).ThenBy(c => c.CustomerID); break;
                case "name_desc": customers = customers.OrderByDescending(c => c.CustomerName).ThenBy(c => c.CustomerID); break;
                case "id_desc": customers = customers.OrderByDescending(c => c.CustomerID); break;
                default: sortOrder = "id_asc"; customers = customers.OrderBy(c => c.CustomerID); break;
            }
            return View(new CustomerSearchVM { Keyword = keyword, SortOrder = sortOrder,
                Customers = customers.ToPagedList(PageHelper.Clamp(page, customers.Count()), 10) });
        }
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var customer = db.Customers.Include(c => c.User).SingleOrDefault(c => c.CustomerID == id.Value);
            if (customer == null) return HttpNotFound();
            ViewBag.OrderCount = db.Orders.Count(o => o.CustomerID == id.Value);
            return View(customer);
        }
    }
}
