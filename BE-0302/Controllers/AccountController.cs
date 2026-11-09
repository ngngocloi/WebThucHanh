using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using BE_0302.Helpers;
using BE_0302.Models;
using BE_0302.Models.ViewModel;

namespace BE_0302.Controllers
{
    public class AccountController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {

                var existingUser = db.Users.SingleOrDefault(u => u.Username == model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại!");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password,
                    UserRole = "C"
                };
                db.Users.Add(user);

                var customer = new Customer
                {
                    CustomerName = model.CustomerName,
                    CustomerEmail = model.CustomerEmail,
                    CustomerPhone = model.CustomerPhone,
                    CustomerAddress = model.CustomerAddress,
                    Username = model.Username
                };
                db.Customers.Add(customer);

                db.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(u => u.Username == model.Username);
                if (user != null)
                {
                    string role = (user.UserRole ?? "").Trim();
                    string storedPwd = (user.Password ?? "").Trim();
                    bool passwordMatch = (storedPwd == model.Password) || PasswordHelper.Verify(model.Password, storedPwd);

                    if (passwordMatch && (role == "C" || role.Equals("Customer", StringComparison.OrdinalIgnoreCase)))
                    {

                        Session["Username"] = user.Username;
                        Session["UserRole"] = user.UserRole;

                        FormsAuthentication.SetAuthCookie(user.Username, false);

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult ProfileInfo()
        {
            string username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var customer = db.Customers.FirstOrDefault(c => c.Username == username);
            if (customer == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileInfoVM
            {
                CustomerID = customer.CustomerID,
                Username = customer.Username,
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                CustomerAddress = customer.CustomerAddress
            };

            return View("ProfileInfo", model);
        }

        [Authorize]
        public new ActionResult Profile()
        {
            return ProfileInfo();
        }

        [Authorize]
        public ActionResult Edit()
        {
            return ProfileInfo();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateProfile(ProfileInfoVM model)
        {
            string username = User.Identity.Name;
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);
            if (customer == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                customer.CustomerName = model.CustomerName;
                customer.CustomerPhone = model.CustomerPhone;
                customer.CustomerEmail = model.CustomerEmail;
                customer.CustomerAddress = model.CustomerAddress;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                return RedirectToAction("ProfileInfo");
            }

            return View("ProfileInfo", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangePassword(ProfileInfoVM model)
        {
            string username = User.Identity.Name;
            var user = db.Users.SingleOrDefault(u => u.Username == username);
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);
            if (user == null || customer == null)
            {
                return RedirectToAction("Login");
            }

            model.CustomerName = customer.CustomerName;
            model.CustomerPhone = customer.CustomerPhone;
            model.CustomerEmail = customer.CustomerEmail;
            model.CustomerAddress = customer.CustomerAddress;
            model.Username = customer.Username;

            string storedPwd = (user.Password ?? "").Trim();
            bool currentMatch = !string.IsNullOrEmpty(model.CurrentPassword) &&
                                ((storedPwd == model.CurrentPassword) || PasswordHelper.Verify(model.CurrentPassword, storedPwd));

            if (!currentMatch)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View("ProfileInfo", model);
            }

            if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                return View("ProfileInfo", model);
            }

            user.Password = model.NewPassword;
            db.SaveChanges();
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ProfileInfo");
        }
    }
}
