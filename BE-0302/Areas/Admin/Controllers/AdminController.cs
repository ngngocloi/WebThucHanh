using System.Data.Entity.Infrastructure;
using System.Web.Mvc;
using BE_0302.Filters;
using BE_0302.Models;

namespace BE_0302.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public abstract class AdminController : Controller
    {
        protected readonly MyStoreEntities db = new MyStoreEntities();
        protected bool SaveChanges()
        {
            try { db.SaveChanges(); return true; }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể lưu dữ liệu. Bản ghi có thể đã thay đổi hoặc đang được sử dụng. Vui lòng tải lại trang và thử lại.");
                return false;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
