using System.Linq;
using System.Web.Mvc;
using BE_0302.Models.ViewModel;

namespace BE_0302.Areas.Admin.Controllers
{
    public class AdminHomeController : AdminController
    {
        public ActionResult Index()
        {
            return View(new DashboardVM
            {
                CategoryCount = db.Categories.Count(), ProductCount = db.Products.Count(),
                CustomerCount = db.Customers.Count(), OrderCount = db.Orders.Count(),
                OrderTotal = db.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0,
                Categories = db.Categories.OrderBy(c => c.CategoryName)
                    .Select(c => new CategoryStatistic { Name = c.CategoryName, Count = c.Products.Count() }).ToList()
            });
        }
    }
}
