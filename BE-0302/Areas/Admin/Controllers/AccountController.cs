using System.Data;
using System.Linq;
using System.Web.Mvc;
using BE_0302.Helpers;
using BE_0302.Models;
using BE_0302.Models.ViewModel;

namespace BE_0302.Areas.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CanSetup = Request.IsLocal && !db.Users.Any(u => u.UserRole == "A");
            return View(new LoginVM());
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(u => u.Username == model.Username && u.UserRole == "A");
                if (user != null && PasswordHelper.Verify(model.Password, user.Password))
                {
                    if (!user.Password.TrimEnd().StartsWith("p1$"))
                    {
                        user.Password = PasswordHelper.Hash(model.Password);
                        db.SaveChanges();
                    }
                    Session.Clear();
                    Session["AdminUsername"] = user.Username;
                    if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                    return RedirectToAction("Index", "AdminHome");
                }
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CanSetup = Request.IsLocal && !db.Users.Any(u => u.UserRole == "A");
            return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
        public ActionResult Setup()
        {
            if (!Request.IsLocal) return new HttpStatusCodeResult(403);
            if (db.Users.Any(u => u.UserRole == "A")) return HttpNotFound();
            return View(new AdminUserVM());
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Setup(AdminUserVM model)
        {
            if (!Request.IsLocal) return new HttpStatusCodeResult(403);
            if (string.IsNullOrEmpty(model.Password)) ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                if (db.Users.Any(u => u.UserRole == "A")) return HttpNotFound();
                if (db.Users.Any(u => u.Username == model.Username))
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                if (ModelState.IsValid)
                {
                    db.Users.Add(new User { Username = model.Username, Password = PasswordHelper.Hash(model.Password), UserRole = "A" });
                    db.SaveChanges();
                    transaction.Commit();
                    TempData["Success"] = "Đã tạo quản trị viên. Bạn có thể đăng nhập.";
                    return RedirectToAction("Login");
                }
            }
            return View(model);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
