using System;
using System.Linq;
using System.Web.Mvc;
using CmsData;
using CmsWeb.Areas.OnlineReg.Models;
using CmsWeb.Code;
using UtilityExtensions;

namespace CmsWeb.Areas.OnlineReg.Controllers
{
    public partial class OnlineRegController
    {
        public ActionResult ManagePledge(string id)
        {
            if (!id.HasValue())
                return Content("bad link");
            ManagePledgesModel m = null;
            var td = Util.TempPeopleId;
            if (td != null)
                m = new ManagePledgesModel(td.ToInt(), id.ToInt());
            else
            {
                var guid = id.ToGuid();
                if (guid == null)
                    return Content("invalid link");
                var ot = CurrentDatabase.OneTimeLinks.SingleOrDefault(oo => oo.Id == guid.Value);
                if (ot == null)
                    return Content("invalid link");
                if (ot.Used)
                    return Content("link used");
                if (ot.Expires.HasValue && ot.Expires < DateTime.Now)
                    return Content("link expired");
                var a = ot.Querystring.Split(',');
                m = new ManagePledgesModel(a[1].ToInt(), a[0].ToInt());
                ot.Used = true;
                CurrentDatabase.SubmitChanges();
            }
            SetHeaders(m.orgid);
            m.Log("Start");
            return View("ManagePledge/Index", m);
        }

        [HttpGet]
        public ActionResult ManageGiving(string id, bool? testing, string campus = "", string funds = "")
        {
            if (!id.HasValue())
            {
                return Message("bad link");
            }
            var orgId = id;
            ManageGivingModel m = null;
            var td = Util.TempPeopleId;

            SetCampusAndDefaultFunds(campus, funds, orgId.ToInt());

            funds = Util.DefaultFunds;

            if (td != null)
            {
                m = new ManageGivingModel(CurrentDatabase.Host, td.ToInt(), orgId.ToInt(), funds);
                if (m.person == null)
                    return Message("person not found");
            }
            else
            {
                var guid = orgId.ToGuid();
                if (guid == null)
                    return Content("invalid link");
                var ot = CurrentDatabase.OneTimeLinks.SingleOrDefault(oo => oo.Id == guid.Value);
                if (ot == null)
                    return Content("invalid link");
#if DEBUG2
#else
                if (ot.Used)
                    return Content("link used");
#endif
                if (ot.Expires.HasValue && ot.Expires < DateTime.Now)
                    return Content("link expired");
                var a = ot.Querystring.Split(',');
                m = new ManageGivingModel(CurrentDatabase.Host, a[1].ToInt(), a[0].ToInt(), funds);
                if (m.person == null)
                    return Message("person not found");
                ot.Used = true;
                CurrentDatabase.SubmitChanges();
            }
            Util.CreditCardOnFile = m.CreditCard;
            Util.ExpiresOnFile = m.Expires;
            if (!m.testing)
                m.testing = testing ?? false;
            SetHeaders(m.orgid);
            m.Log("Start");
            return View("ManageGiving/Setup", m);
        }

        [HttpPost]
        public ActionResult ManageGiving(ManageGivingModel m)
        {
            m.SetCurrentDatabase(CurrentDatabase);
            SetHeaders(m.orgid);

            // only validate if the amounts are greater than zero.
            if (m.FundItemsChosen().Sum(f => f.amt) > 0)
            {
                m.ValidateModel(ModelState);
                if (!ModelState.IsValid)
                {
                    if (m.person == null)
                    {
                        return Message("person not found");
                    }
                    m.total = 0;
                    foreach (var ff in m.FundItemsChosen())
                    {
                        m.total += ff.amt;
                    }
                    return View("ManageGiving/Setup", m);
                }
            }
            else
            {
                ModelState.AddModelError("funds", "You must choose at least one fund to give to.");
                return View("ManageGiving/Setup", m);
            }
            if (CurrentDatabase.Setting("UseRecaptchaForManageGiving"))
            {
                if (!GoogleRecaptcha.IsValidResponse(HttpContext, CurrentDatabase))
                {
                    ModelState.AddModelError("TranId", "ReCaptcha validation failed.");
                    return View("ManageGiving/Setup", m);
                }
            }

            try
            {
                m.Update();
            }
            catch (Exception ex)
            {
                if (ex.Message == "InvalidVaultId")
                {
                    m = ClearPaymentInfo(m, ModelState);
                }
                else
                {
                    ModelState.AddModelError("form", ex.Message);
                }
            }
            if (!ModelState.IsValid)
            {
                return View("ManageGiving/Setup", m);
            }

            RequestManager.SessionProvider.Add("managegiving", m);
            return Redirect("/OnlineReg/ConfirmRecurringGiving");
        }

        private ManageGivingModel ClearPaymentInfo(ManageGivingModel m, ModelStateDictionary modelState)
        {            
            m.CreditCard = string.Empty;
            m.Expires = string.Empty;
            m.Routing = string.Empty;
            m.Account = string.Empty;
            m.CVV = string.Empty;
            modelState.Clear();
            modelState.AddModelError("form", "Please insert your payment information.");
            return m;
        }

        public ActionResult ConfirmRecurringGiving()
        {
            var m = RequestManager.SessionProvider.Get<ManageGivingModel>("managegiving");
            if (m == null)
            {
                return Content("No active registration");
            }

            m.SetCurrentDatabase(CurrentDatabase);

            if (Util.IsDebug())
            {
                m.testing = true;
            }

            if (!m.ManagedGivingStopped)
            {
                m.Confirm(this);
            }

            SetHeaders(m.orgid);
            OnlineRegModel.LogOutOfOnlineReg();
            RequestManager.SessionProvider.Clear();

            m.Log("Confirm");
            return View("ManageGiving/Confirm", m);
        }

        [HttpPost]
        public ActionResult ConfirmPledge(ManagePledgesModel m)
        {
            m.Confirm();
            SetHeaders(m.orgid);
            OnlineRegModel.LogOutOfOnlineReg();
            RequestManager.SessionProvider.Clear();

            m.Log("Confirm");
            return View("ManagePledge/Confirm", m);
        }

        [HttpPost]
        public ActionResult RemoveManagedGiving(int peopleId, int orgId)
        {
            var m = new ManageGivingModel(CurrentDatabase.Host, peopleId, orgId);
            m.CancelManagedGiving(peopleId);
            m.ThankYouMessage = "Your recurring giving has been stopped.";

            m.Log("Remove");
            RequestManager.SessionProvider.Add("managegiving", m);
            return Json(new { Url = Url.Action("ConfirmRecurringGiving") });
        }

        private void SetCampusAndDefaultFunds(string campus, string funds, int orgId)
        {
            if (!string.IsNullOrWhiteSpace(campus))
            {
                RequestManager.SessionProvider.Add($"Campus-{orgId}", campus);
            }
            if (!string.IsNullOrWhiteSpace(funds))
            {
                Util.DefaultFunds = funds;
            }
        }
    }
}
