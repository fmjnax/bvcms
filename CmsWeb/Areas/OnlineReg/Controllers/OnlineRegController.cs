using CmsData;
using CmsData.Codes;
using CmsWeb.Areas.Manage.Controllers;
using CmsWeb.Areas.Manage.Models;
using CmsWeb.Areas.OnlineReg.Models;
using CmsWeb.Code;
using CmsWeb.Membership;
using CmsWeb.Models;
using Elmah;
using ImageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using UtilityExtensions;

namespace CmsWeb.Areas.OnlineReg.Controllers
{
    [ValidateInput(false)]
    [RouteArea("OnlineReg", AreaPrefix = "OnlineReg"), Route("{action}/{id?}")]
    public partial class OnlineRegController : CmsController
    {
        private string fromMethod;

        [HttpGet]
        [Route("~/OnlineReg/Index/{id:int}")]
        [Route("~/OnlineReg/{id:int}")]
        public ActionResult Index(int? id, bool? testing, string email, bool? login, string registertag, bool? showfamily, int? goerid, int? gsid, string source, int? pledgeFund)
        {
            Response.NoCache();

            var m = new OnlineRegModel(Request, CurrentDatabase, id, testing, email, login, source);
            var isMissionTrip = (m.org?.IsMissionTrip).GetValueOrDefault();

            if (isMissionTrip)
            {
                m.ProcessType = PaymentProcessTypes.OnlineRegistration;
            }
            else
            {
                AssignPaymentProcessType(ref m);
            }

            if (pledgeFund != null)            
                m.pledgeFundId = pledgeFund.Value;            

            SetHeaders(m);

            int? GatewayId = MultipleGatewayUtils.GatewayId(CurrentDatabase, m.ProcessType);
            var gatewayRequired = (m.PayAmount() > 0 || m.ProcessType == PaymentProcessTypes.OneTimeGiving || m.ProcessType == PaymentProcessTypes.RecurringGiving);

            if (GatewayId.IsNull() && gatewayRequired)
            {
                return View("OnePageGiving/NotConfigured");
            }

            if ((int)GatewayTypes.Pushpay == GatewayId && string.IsNullOrEmpty(MultipleGatewayUtils.Setting(CurrentDatabase, "PushpayMerchant", "", (int)m.ProcessType)))
            {
                ViewBag.Header = m.Header;
                ViewBag.Instructions = m.Instructions;
                return View("OnePageGiving/NotConfigured");
            }

            if (m.ManageGiving())
            {
                RequestManager.SessionProvider.Add($"Campus-{m.Orgid}",
                    m.Campus = Request.QueryString["campus"]);
                m.DefaultFunds = Util.DefaultFunds = Request.QueryString["funds"];
            }

            if (isMissionTrip)
            {
                if (gsid != null || goerid != null)
                {
                    m.PrepareMissionTrip(gsid, goerid);
                }
            }

            var pid = m.CheckRegisterLink(registertag);
            if (m.NotActive())
            {
                return View("OnePageGiving/NotActive", m);
            }
            if (m.MissionTripSelfSupportPaylink.HasValue() && m.GoerId > 0)
            {
                return Redirect(m.MissionTripSelfSupportPaylink);
            }

            return RouteRegistration(m, pid, showfamily);
        }

        private void AssignPaymentProcessType(ref OnlineRegModel m)
        {
            switch (m.org?.RegistrationTypeId.GetValueOrDefault())
            {
                case RegistrationTypeCode.OnlineGiving:
                    m.ProcessType = PaymentProcessTypes.OneTimeGiving;
                    break;
                case RegistrationTypeCode.ManageGiving:
                    m.ProcessType = PaymentProcessTypes.RecurringGiving;
                    break;
                default:
                    m.ProcessType = PaymentProcessTypes.OnlineRegistration;
                    break;
            }
        }

        [HttpPost]
        public ActionResult Login(OnlineRegModel m)
        {
            fromMethod = "Login";
            var ret = AccountModel.AuthenticateLogon(m.username, m.password, Request, CurrentDatabase, CurrentImageDatabase);

            if (ret.ErrorMessage.HasValue())
            {
                ModelState.AddModelError("authentication", ret.ErrorMessage);
                return FlowList(m);
            }
            else  if (MembershipService.ShouldPromptForTwoFactorAuthentication(ret.User, CurrentDatabase, Request))
            {
                Util.MFAUserId = ret.User.UserId;
                ViewData["hasshell"] = true;
                var orgId = m.Orgid ?? m.masterorgid;
                return View("Auth", new AccountInfo {
                    UsernameOrEmail = ret.User.Username,
                    ReturnUrl = RouteExistingRegistration(m) ?? $"/OnlineReg/{orgId}"
                });
            }
            else
            {
                AccountModel.FinishLogin(ret.User.Username, CurrentDatabase, CurrentImageDatabase);
                if (ret.User.UserId.Equals(Util.MFAUserId))
                {
                    MembershipService.SaveTwoFactorAuthenticationToken(CurrentDatabase, Response);
                    Util.MFAUserId = null;
                }
            }
            Util.OnlineRegLogin = true;

            if (m.Orgid == Util.CreateAccountCode)
            {
                DbUtil.LogActivity("OnlineReg CreateAccount Existing", peopleid: ret.User.PeopleId, datumId: m.DatumId);
                return Content("/Person2/" + ret.User.PeopleId); // they already have an account, so take them to their page
            }
            m.UserPeopleId = ret.User.PeopleId;
            var route = RouteSpecialLogin(m);
            if (route != null)
            {
                return route;
            }

            m.HistoryAdd("login");
            if (m.org != null && m.org.IsMissionTrip == true && m.SupportMissionTrip)
            {
                PrepareFirstRegistrant(ref m, m.UserPeopleId.Value, false, out OnlineRegPersonModel p);
            }
            return FlowList(m);
        }

        private void PrepareFirstRegistrant(ref OnlineRegModel m, int pid, bool? showfamily, out OnlineRegPersonModel p)
        {
            p = null;
            if (showfamily != true)
            {
                // No need to pick family, so prepare first registrant ready to answer questions
                p = m.LoadExistingPerson(pid, 0);
                if (p == null)
                    throw new Exception($"No person found with PeopleId = {pid}");
                p.ProcessType = m.ProcessType;
                p.ValidateModelForFind(ModelState, 0);
                if (m.masterorg == null)
                {
                    if (m.List.Count == 0)
                        m.List.Add(p);
                    else
                        m.List[0] = p;
                }
            }
        }

        [HttpPost]
        public ActionResult NoLogin(OnlineRegModel m)
        {
            fromMethod = "NoLogin";
            // Clicked the register without logging in link
            m.nologin = true;
            m.CreateAnonymousList();
            m.Log("NoLogin");
            return FlowList(m);
        }

        [HttpPost]
        public ActionResult YesLogin(OnlineRegModel m)
        {
            fromMethod = "YesLogin";
            // clicked the Login Here button
            m.HistoryAdd("yeslogin");
            m.nologin = false;
            m.List = new List<OnlineRegPersonModel>();
#if DEBUG
            m.username = "David";
#endif
            return FlowList(m);
        }

        [HttpPost]
        public ActionResult RegisterFamilyMember(int id, OnlineRegModel m)
        {
            // got here by clicking on a link in the Family list
            var msg = m.CheckExpiredOrCompleted();
            if (msg.HasValue())
            {
                return PageMessage(msg);
            }

            fromMethod = "Register";

            m.StartRegistrationForFamilyMember(id, ModelState);

            // show errors or take them to the Questions page
            return FlowList(m);
        }

        [HttpPost]
        public ActionResult Cancel(int id, OnlineRegModel m)
        {
            // After clicking Cancel, remove a person from the completed registrants list
            fromMethod = "Cancel";
            m.CancelRegistrant(id);
            return FlowList(m);
        }

        [HttpPost]
        public ActionResult FindRecord(int id, OnlineRegModel m)
        {
            // Anonymous person clicks submit to find their record
            var msg = m.CheckExpiredOrCompleted();
            if (msg.HasValue())
            {
                return PageMessage(msg);
            }

            fromMethod = "FindRecord";
            m.HistoryAdd("FindRecord id=" + id);
            if (id >= m.List.Count)
            {
                return FlowList(m);
            }

            var p = m.GetFreshFindInfo(id);

            if (p.NeedsToChooseClass())
            {
                p.RegistrantProblem = "Please Make Selection Above";
                return FlowList(m);
            }
            p.ValidateModelForFind(ModelState, id);
            if (!ModelState.IsValid)
            {
                return FlowList(m);
            }

            if (p.AnonymousReRegistrant())
            {
                return View("Continue/ConfirmReregister", m); // send email with link to reg-register
            }

            if (p.IsSpecialReg())
            {
                p.QuestionsOK = true;
            }
            else if (p.RegistrationFull())
            {
                m.Log("Closed");
                ModelState.AddModelError(m.GetNameFor(mm => mm.List[id].DateOfBirth), "Sorry, but registration is closed.");
            }

            p.FillPriorInfo();
            p.SetSpecialFee();

            if (!ModelState.IsValid || p.count == 1)
            {
                return FlowList(m);
            }

            // form is ok but not found, so show AddressGenderMarital Form
            p.PrepareToAddNewPerson(ModelState, id);
            p.Found = false;
            return FlowList(m);
        }


        [HttpPost]
        public ActionResult SubmitNew(int id, OnlineRegModel m)
        {
            // Submit from AddressMaritalGenderForm
            var msg = m.CheckExpiredOrCompleted();
            if (msg.HasValue())
            {
                return PageMessage(msg);
            }

            fromMethod = "SubmitNew";
            ModelState.Clear();
            m.HistoryAdd("SubmitNew id=" + id);
            var p = m.List[id];
            if (p.ComputesOrganizationByAge())
            {
                p.orgid = null; // forget any previous information about selected org, may have new information like gender
            }

            p.ValidateModelForNew(ModelState, id);

            SetHeaders(m);
            var ret = p.AddNew(ModelState, id);
            return ret.HasValue()
                ? View(ret, m)
                : FlowList(m);
        }

        [HttpPost]
        public ActionResult SubmitQuestions(int id, OnlineRegModel m)
        {
            var ret = m.CheckExpiredOrCompleted();
            if (ret.HasValue())
            {
                return PageMessage(ret);
            }

            fromMethod = "SubmitQuestions";
            m.HistoryAdd("SubmitQuestions id=" + id);
            if (m.List.Count <= id)
            {
                return Content("<p style='color:red'>error: cannot find person on submit other info</p>");
            }

            bool supportGoerRequired = CurrentDatabase.Setting("MissionSupportRequiredGoer", "false").ToBool();
            m.List[id].ValidateModelQuestions(ModelState, id, supportGoerRequired);
            return FlowList(m);
        }

        [HttpPost]
        public JsonResult UploadDocument()
        {
            var file = Request.Files[0];
            Int32.TryParse(Request["registrantId"], out int registrantId);
            Int32.TryParse(Request["orgId"], out int orgId);
            var docname = Request["docname"];
            var email = Request["email"];
            var lastName = Request["lastName"];
            var firstName = Request["firstName"];
            StoreDocument(file, docname, registrantId, orgId, email, lastName, firstName);
            return Json(new { file.FileName });
        }

        private void StoreDocument(HttpPostedFileBase file, string docname, int? registrantId, int orgId, string email, string lastName, string firstName)
        {
            var person = CurrentDatabase.People.SingleOrDefault(p => p.PeopleId == registrantId);

            if (person == null)
                DocumentsHelper.CreateTemporaryDocument(CurrentDatabase, CurrentImageDatabase, docname, orgId, email, lastName, firstName, file);
            else
                DocumentsHelper.CreateMemberDocument(CurrentDatabase, CurrentImageDatabase, docname, orgId, person, file);
        }

        [HttpPost]
        public ActionResult AddAnotherPerson(OnlineRegModel m)
        {
            var ret = m.CheckExpiredOrCompleted();
            if (ret.HasValue())
            {
                return PageMessage(ret);
            }

            fromMethod = "AddAnotherPerson";
            m.HistoryAdd("AddAnotherPerson");
            m.ParseSettings();
            if (!ModelState.IsValid)
            {
                return FlowList(m);
            }

            m.List.Add(new OnlineRegPersonModel(CurrentDatabase)
            {
                orgid = m.Orgid,
                masterorgid = m.masterorgid,
            });
            return FlowList(m);
        }

        [HttpPost]
        public ActionResult AskDonation(OnlineRegModel m)
        {
            m.HistoryAdd("AskDonation");
            if (m.List.Count == 0)
            {
                m.Log("AskDonationError NoRegistrants");
                return Content("Can't find any registrants");
            }
            m.RemoveLastRegistrantIfEmpty();
            SetHeaders(m);
            return View("Other/AskDonation", m);
        }
        [HttpPost]
        public ActionResult PostDonation(OnlineRegModel m)
        {
            if (m.donor == null && m.donation > 0)
            {
                ModelState.AddModelError("donation", "Please indicate who is the donor");
                SetHeaders(m);
                return View("Other/AskDonation", m);
            }
            SaveOnlineRegModelInSession(m);
            return Redirect("/OnlineReg/CompleteRegistration");
        }

        [HttpPost]
        public ActionResult CompleteRegistration(OnlineRegModel m)
        {
            if (m.org?.RegistrationTypeId == RegistrationTypeCode.SpecialJavascript)
            {
                m.List[0].SpecialTest = SpecialRegModel.ParseResults(Request.Form);
            }
            SaveOnlineRegModelInSession(m);
            return Redirect("/OnlineReg/CompleteRegistration");
        }

        private void SaveOnlineRegModelInSession(OnlineRegModel m)
        {
            CurrentDatabase.SessionValues.DeleteAllOnSubmit(
                CurrentDatabase.SessionValues.Where(v => v.SessionId == Session.SessionID && v.Name == "onlineregmodel"));
            var xml = Util.Serialize(m);
            if (!xml.HasValue())
            {
                throw new Exception("Could not serialize the OnlineReg model");
            }

            CurrentDatabase.SessionValues.InsertOnSubmit(new SessionValue { SessionId = Session.SessionID, Name = "onlineregmodel", Value = xml });
            CurrentDatabase.SubmitChanges();
        }

        private OnlineRegModel ReadOnlineRegModelFromSession()
        {
            var s = CurrentDatabase.SessionValues.Where(v => v.SessionId == Session.SessionID && v.Name == "onlineregmodel").FirstOrDefault();
            if (s?.Value == null)
            {
                DbUtil.LogActivity("OnlineReg Error PageRefreshNotAllowed");
                return null;
            }
            var m = Util.DeSerialize<OnlineRegModel>(s.Value);
            CurrentDatabase.SessionValues.DeleteAllOnSubmit(
                CurrentDatabase.SessionValues.Where(v => v.SessionId == Session.SessionID && v.Name == "onlineregmodel"));
            return m;
        }

        [HttpGet]
        public ActionResult CompleteRegistration()
        {
            Response.NoCache();
            var m = ReadOnlineRegModelFromSession();
            if (m == null)
            {
                return Message("Registration cannot be completed after a page refresh.");
            }
            m.CurrentDatabase = CurrentDatabase;
            var msg = m.CheckExpiredOrCompleted();
            if (msg.HasValue())
            {
                return Message(msg);
            }

            var ret = m.CompleteRegistration(this);

            int? GatewayId = MultipleGatewayUtils.GatewayId(CurrentDatabase, m.ProcessType);

            if (ret.Route == RouteType.Payment && (int)GatewayTypes.Pushpay == GatewayId)
            {
                m.UpdateDatum();
                RequestManager.SessionProvider.Add("PaymentProcessType", PaymentProcessTypes.OnlineRegistration.ToInt().ToString());
                return Redirect($"/Pushpay/Registration/{m.DatumId}");
            }

            switch (ret.Route)
            {
                case RouteType.Error:
                    m.Log(ret.Message);
                    return Message(ret.Message);
                case RouteType.Action:
                    return View(ret.View);
                case RouteType.Redirect:
                    return RedirectToAction(ret.View, ret.RouteData);
                case RouteType.Terms:
                    return View(ret.View, m);
                case RouteType.Payment:
                    return View(ret.View, ret.PaymentForm);
            }
            m.Log("BadRouteOnCompleteRegistration");
            return Message("unexpected value on CompleteRegistration");
        }

        [HttpPost]
        public JsonResult CityState(string id)
        {
            var z = CurrentDatabase.ZipCodes.SingleOrDefault(zc => zc.Zip == id);
            if (z == null)
            {
                return Json(null);
            }

            return Json(new { city = z.City.Trim(), state = z.State });
        }

        public ActionResult Timeout(string ret)
        {
            FormsAuthentication.SignOut();
            RequestManager.SessionProvider.Clear();
            Session.Abandon();
            ViewBag.Url = ret;
            return View("Other/Timeout");
        }

        private ActionResult FlowList(OnlineRegModel m)
        {
            try
            {
                m.UpdateDatum();
                m.Log(fromMethod);
                var content = ViewExtensions2.RenderPartialViewToString(this, "Flow2/List", m);
                return Content(content);
            }
            catch (Exception ex)
            {
                return ErrorResult(m, ex, "In " + fromMethod + "<br>" + ex.Message);
            }
        }

        private ActionResult ErrorResult(OnlineRegModel m, Exception ex, string errorDisplay)
        {
            // ReSharper disable once EmptyGeneralCatchClause
            try
            {
                m.UpdateDatum();
            }
            catch
            {
            }

            var ex2 = new Exception($"{errorDisplay}, {CurrentDatabase.ServerLink("/OnlineReg/RegPeople/") + m.DatumId}", ex);
            ErrorSignal.FromCurrentContext().Raise(ex2);
            m.Log(ex2.Message);
            Util.TempError = errorDisplay;
            ViewBag.stack = ex.StackTrace;
            return Content("/Error/");
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }

            ErrorSignal.FromCurrentContext().Raise(filterContext.Exception);
            DbUtil.LogActivity("OnlineReg Error:" + filterContext.Exception.Message);
            filterContext.Result = Message(filterContext.Exception.Message, filterContext.Exception.StackTrace);
            filterContext.ExceptionHandled = true;
        }

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            requestContext.HttpContext.Items["controller"] = this;
        }

        [HttpGet]
        [Route("~/OnlineReg/{id:int}/Giving/")]
        [Route("~/OnlineReg/{id:int}/Giving/{goerid:int}")]
        public ActionResult Giving(int id, int? goerid, int? gsid)
        {
            var m = new OnlineRegModel(Request, CurrentDatabase, id, false, null, null, null);
            if (m.org != null && m.org.IsMissionTrip == true && m.org.TripFundingPagesEnable == true)
            {
                m.PrepareMissionTrip(gsid, goerid);
            }
            else
            {
                return new HttpNotFoundResult();
            }

            SetHeaders(m);
            if (m.MissionTripCost == null)
            {
                // goer specified isn't part of this trip
                return new HttpNotFoundResult();
            }
            if (!m.URL.HasValue() || m.URL.Contains("False"))
            {
                m.URL = CurrentDatabase.ServerLink($"/OnlineReg/{id}/Giving/{goerid}");
            }

            var currentUserId = CurrentDatabase.UserPeopleId;
            if (currentUserId != null && currentUserId == goerid)
            {
                return View("Giving/Goer", m);
            }
            else if (m.org.TripFundingPagesPublic)
            {
                return View("Giving/Guest", m);
            }
            else
            {
                return new HttpNotFoundResult();
            }
        }
    }
}
