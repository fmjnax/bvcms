using CmsData;
using CmsData.Classes.RoleChecker;
using CmsWeb.Areas.Manage.Controllers;
using CmsWeb.Code;
using CmsWeb.Lifecycle;
using CmsWeb.Models;
using OfficeOpenXml;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using UtilityExtensions;

namespace CmsWeb
{
    [MyRequireHttps]
    public class CmsController : CmsControllerNoHttps
    {
        public CmsController(IRequestManager requestManager) : base(requestManager)
        {
        }
    }

    public class CmsControllerNoHttps : CMSBaseController
    {
        public CmsControllerNoHttps(IRequestManager requestManager) : base(requestManager)
        {

        }

        protected override void HandleUnknownAction(string actionName)
        {
            //base.HandleUnknownAction(actionName);
            throw new HttpException(404, "404");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            Util.Helpfile = $"_{filterContext.ActionDescriptor.ControllerDescriptor.ControllerName}_{filterContext.ActionDescriptor.ActionName}";
            CurrentDatabase.UpdateLastActivity(CurrentDatabase.UserId);
            if (AccountController.TryImpersonate(CurrentDatabase, CurrentImageDatabase) || filterContext.ActionParameters.Values.Equals("/Pushpay/true"))
            {
                var returnUrl = Request.QueryString["returnUrl"];
                if (returnUrl.HasValue() && Url.IsLocalUrl(returnUrl))
                {
                    filterContext.Result = Redirect(returnUrl);
                }
                else
                {
                    filterContext.Result = Redirect(Request.RawUrl);
                }
            }
            HttpContext.Response.Headers.Add("X-Robots-Tag", "noindex");
            HttpContext.Response.Headers.Add("X-Robots-Tag", "unavailable after: 1 Jan 2017 01:00:00 CST");
        }

        public string AuthenticateDeveloper(bool log = false, string addrole = "", string altrole = "")
        {
            return AuthHelper.AuthenticateDeveloper(HttpContextFactory.Current, log, addrole, altrole).Message;
        }

        public ViewResult Message(string text, string stacktrace = null)
        {
            return View("Message", model: new ErrorMessage { text = text, stacktrace = stacktrace ?? Environment.StackTrace });
        }
        public ViewResult PageMessage(string text, string title = "Error", string alert = "danger")
        {
            ViewBag.Alert = alert;
            ViewBag.Message = text;
            return View("PageMessage");
        }

        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonNetResult { Data = data, ContentType = contentType, ContentEncoding = contentEncoding, JsonRequestBehavior = behavior };
        }

        public string RenderRazorViewToString(string viewName, object model)
        {
            return ViewExtensions2.RenderPartialViewToString(this, viewName, model);
        }
    }

    [MyRequireHttps]
    public class CmsStaffController : CMSBaseController
    {
        public CmsStaffController(IRequestManager requestManager) : base(requestManager)
        {
        }

        public bool NoCheckRole { get; set; }

        protected override void HandleUnknownAction(string actionName)
        {
            //base.HandleUnknownAction(actionName);
            throw new HttpException(404, "404");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            NoCheckRole = NoCheckRole ||
                          (filterContext.RouteData.Values["Controller"].ToString() == "Email" && CurrentDatabase.Setting("UX-AllowMyDataUserEmails")) ||
                          (filterContext.RouteData.Values["Controller"].ToString() == "OrgMemberDialog" && filterContext.RouteData.Values["Action"].ToString() == "Drop"
                            && CurrentDatabase.Setting("UX-AllowMyDataUserLeaveOrg") && CurrentDatabase.UserPeopleId.ToString() == filterContext.RequestContext?.HttpContext?.Request?.Params["PeopleId"]);

            if (!User.Identity.IsAuthenticated)
            {
                var s = "/Logon?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl);
                if (Request.QueryString.Count > 0)
                {
                    s += "&" + Request.QueryString.ToString();
                }

                filterContext.Result = Redirect(s);
            }
            else if (!NoCheckRole)
            {
                var r = AccountModel.CheckAccessRole(CurrentDatabase, User.Identity.Name);
                if (r.HasValue())
                {
                    filterContext.Result = Redirect(r);
                }
            }

            var disableHomePageForOrgLeaders = CurrentDatabase.Setting("UX-DisableHomePageForOrgLeaders");
            if (!disableHomePageForOrgLeaders)
            {
                disableHomePageForOrgLeaders = RoleChecker.HasSetting(SettingName.DisableHomePage, false);
            }

            var contr = filterContext.RouteData.Values["Controller"].ToString();
            var act = filterContext.RouteData.Values["Action"].ToString();
            var orgleaderonly = User.IsInRole("OrgLeadersOnly");
            if (contr == "Home" && act == "Index" &&
                disableHomePageForOrgLeaders && orgleaderonly)
            {
                Util2.OrgLeadersOnly = true;
                CurrentDatabase.SetOrgLeadersOnly();
                filterContext.Result = Redirect($"/Person2/{CurrentDatabase.UserPeopleId}");
            }
            else if (orgleaderonly && Util2.OrgLeadersOnly == false)
            {
                Util2.OrgLeadersOnly = true;
                CurrentDatabase.SetOrgLeadersOnly();
            }

            if (contr == "Home" && act == "Index" && RoleChecker.HasSetting(SettingName.DisableHomePage, false))
            {
                filterContext.Result = Redirect($"/Person2/{CurrentDatabase.UserPeopleId}");
            }

            base.OnActionExecuting(filterContext);
            Util.Helpfile = $"_{filterContext.ActionDescriptor.ControllerDescriptor.ControllerName}_{filterContext.ActionDescriptor.ActionName}";
            if (CurrentDatabase.UserId == 0 && User.Identity.IsAuthenticated)
            {
                AccountModel.SetUserInfo(CurrentDatabase, CurrentImageDatabase, User.Identity.Name);
            }
            CurrentDatabase.UpdateLastActivity(CurrentDatabase.UserId);
            HttpContext.Response.Headers.Add("X-Robots-Tag", "noindex");
            HttpContext.Response.Headers.Add("X-Robots-Tag", "unavailable after: 1 Jan 2017 01:00:00 CST");
        }

        public static string ErrorUrl(string message)
        {
            return $"/Home/ShowError/?error={HttpContextFactory.Current.Server.UrlEncode(message)}&url={HttpContextFactory.Current.Request.Url.OriginalString}";
        }

        public ActionResult RedirectShowError(string message)
        {
            return new RedirectResult(ErrorUrl(message));
        }

        public ViewResult Message(string text)
        {
            string stacktrace = Environment.StackTrace;
            return View("Message", model: new ErrorMessage { text = text, stacktrace = stacktrace });
        }
        public ViewResult PageMessage(string text, string title = "Error", string alert = "danger")
        {
            ViewBag.Title = title;
            ViewBag.PageHeader = title;
            ViewBag.Alert = alert;
            ViewBag.Message = text;
            return View("PageMessage");
        }

        public ViewResult Message2(string text)
        {
            return View("Message2", model: text);
        }
        public ActionResult Message(Exception ex)
        {
            var userinput = ex as UserInputException;
            if (userinput != null)
            {
                ViewBag.Message = ex.Message;
                return View("PageError");
            }
            throw ex;
        }
        public ActionResult AjaxErrorMessage(Exception ex)
        {
            var userinput = ex as UserInputException;
            if (userinput == null)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return Content($"<strong>Failed!</strong> {ex.Message}");
        }
    }

    [MyRequireHttps]
    public class CmsStaffAsyncController : CMSBaseController
    {
        public bool NoCheckRole { get; set; }

        public CmsStaffAsyncController(IRequestManager requestManager) : base(requestManager)
        {
        }

        protected override void HandleUnknownAction(string actionName)
        {
            //base.HandleUnknownAction(actionName);
            throw new HttpException(404, "404");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var s = "/Logon?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl);
                if (Request.QueryString.Count > 0)
                {
                    s += "&" + Request.QueryString.ToString();
                }

                filterContext.Result = Redirect(s);
            }
            else if (!NoCheckRole)
            {
                var r = AccountModel.CheckAccessRole(CurrentDatabase, User.Identity.Name);
                if (r.HasValue())
                {
                    filterContext.Result = Redirect(r);
                }
            }
            base.OnActionExecuting(filterContext);
            Util.Helpfile = $"_{filterContext.ActionDescriptor.ControllerDescriptor.ControllerName}_{filterContext.ActionDescriptor.ActionName}";
            CurrentDatabase.UpdateLastActivity(CurrentDatabase.UserId);
        }
        public ViewResult Message(string text)
        {
            string stacktrace = Environment.StackTrace;
            return View("Message", model: new ErrorMessage { text = text, stacktrace = stacktrace });
        }
    }

    public class RequireBasicAuthentication : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var req = filterContext.HttpContext.Request;
            if (!req.Headers["Authorization"].HasValue() && !req.Headers["username"].HasValue())
            {
                var res = filterContext.HttpContext.Response;
                res.StatusCode = 401;
                res.AddHeader("WWW-Authenticate", $"Basic realm=\"{DbUtil.Db.Host}\"");
                res.End();
            }
        }
    }

    public class SessionExpire : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
        }
    }

    public class EpplusResult : ActionResult
    {
        private ExcelPackage pkg;
        private readonly string fn;

        public EpplusResult(ExcelPackage pkg, string fn)
        {
            this.pkg = pkg;
            this.fn = fn;
        }

        public EpplusResult(string fn)
        {
            pkg = new ExcelPackage();
            pkg.Workbook.Worksheets.Add("Sheet1");
            this.fn = fn;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.Clear();
            context.HttpContext.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            context.HttpContext.Response.AddHeader("Content-Disposition", "attachment;filename=" + fn);
            pkg.SaveAs(context.HttpContext.Response.OutputStream);
        }
    }

    public class MyRequireHttpsAttribute : RequireHttpsAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            if (filterContext.HttpContext != null)
            {
                if (filterContext.HttpContext.Request.IsLocal)
                {
                    return;
                }

                if (ConfigurationManager.AppSettings["INSERT_X-FORWARDED-PROTO"] == "true")
                {
                    if (filterContext.HttpContext.Request.Headers["X-Forwarded-Proto"] == "http")
                    {
                        var s = filterContext.HttpContext.Request.Url.ToString().Replace("http:", "https:");
                        filterContext.Result = new RedirectResult(s);
                    }
                    return;
                }
                else if (!ConfigurationManager.AppSettings["cmshost"].StartsWith("https:"))
                {
                    return;
                }
            }
            base.OnAuthorization(filterContext);
        }

        public static bool NeedRedirect(HttpRequestBase Request)
        {
            if (ConfigurationManager.AppSettings["INSERT_X-FORWARDED-PROTO"] == "true")
            {
                return Request.Headers["X-Forwarded-Proto"] == "http";
            }

            return ConfigurationManager.AppSettings["cmshost"].StartsWith("https:");
        }
    }
}
