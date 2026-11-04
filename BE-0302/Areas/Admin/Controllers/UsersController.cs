using System.Data;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BE_0302.Helpers;
using BE_0302.Models;
using BE_0302.Models.ViewModel;
using PagedList;

namespace BE_0302.Areas.Admin.Controllers
{
    public class UsersController : AdminController
    {
        public ActionResult Index(string keyword, int? page)
        {
            keyword = (keyword ?? "").Trim();
            var users = db.Users.Where(u => u.UserRole == "A");
            if (keyword.Length > 0) users = users.Where(u => u.Username.Contains(keyword));
            return View(new AdminSearchVM { Keyword = keyword,
                Users = users.OrderBy(u => u.Username).ToPagedList(PageHelper.Clamp(page, users.Count()), 10) });
        }
        public ActionResult Details(string id) { return UserView(id); }
        public ActionResult Delete(string id) { return UserView(id); }
        private ActionResult UserView(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var user = db.Users.SingleOrDefault(u => u.Username == id && u.UserRole == "A");
            if (user == null) return HttpNotFound();
            return View(user);
        }
        public ActionResult Create() { return View(new AdminUserVM()); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(AdminUserVM model)
        {
            if (string.IsNullOrEmpty(model.Password)) ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            if (db.Users.Any(u => u.Username == model.Username)) ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            if (ModelState.IsValid)
            {
                db.Users.Add(new User { Username = model.Username, Password = PasswordHelper.Hash(model.Password), UserRole = "A" });
                if (SaveChanges()) return RedirectToAction("Index");
            }
            return View(model);
        }
        public ActionResult Edit(string id)
        {
            var user = db.Users.SingleOrDefault(u => u.Username == id && u.UserRole == "A");
            if (user == null) return HttpNotFound();
            return View(new AdminUserVM { Username = user.Username });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string id, AdminUserVM model)
        {
            var user = db.Users.SingleOrDefault(u => u.Username == id && u.UserRole == "A");
            if (user == null) return HttpNotFound();
            if (model.Username != user.Username) ModelState.AddModelError("Username", "Không thể đổi tên đăng nhập.");
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.Password)) user.Password = PasswordHelper.Hash(model.Password);
                if (SaveChanges()) return RedirectToAction("Index");
            }
            return View(model);
        }
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var user = db.Users.SingleOrDefault(u => u.Username == id && u.UserRole == "A");
                if (user == null) return HttpNotFound();
                if (user.Username == (string)Session["AdminUsername"])
                    ModelState.AddModelError("", "Bạn không thể xóa tài khoản đang đăng nhập.");
                else if (db.Users.Count(u => u.UserRole == "A") <= 1)
                    ModelState.AddModelError("", "Cần giữ ít nhất một tài khoản quản trị.");
                else if (db.Customers.Any(c => c.Username == user.Username))
                    ModelState.AddModelError("", "Tài khoản đang liên kết với khách hàng, không thể xóa.");
                else
                {
                    db.Users.Remove(user);
                    if (SaveChanges())
                    {
                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                }
                return View(user);
            }
        }
    }
}
