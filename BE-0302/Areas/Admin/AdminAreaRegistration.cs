using System.Web.Mvc;

namespace BE_0302.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "Admin_default",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new { controller = "AdminHome", action = "Index", id = UrlParameter.Optional },
              namespaces: new[] { "BE_0302.Areas.Admin.Controllers" }
            ) ;
        }
    }
}
