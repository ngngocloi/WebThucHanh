using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using BE_0302.Models;

namespace BE_0302.Filters
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase context)
        {
            string username = context.Session["AdminUsername"] as string;
            if (string.IsNullOrEmpty(username)) return false;
            using (var db = new MyStoreEntities())
                return db.Users.Any(u => u.Username == username && u.UserRole == "A");
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext context)
        {
            context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
            {
                area = "Admin", controller = "Account", action = "Login",
                returnUrl = context.HttpContext.Request.RawUrl
            }));
        }
        public override void OnAuthorization(AuthorizationContext context)
        {
            base.OnAuthorization(context);
            context.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.HttpContext.Response.Cache.SetNoStore();
        }
    }
}
