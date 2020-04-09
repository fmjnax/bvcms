using CmsData;
using System;
using System.Linq;
using System.Web;
using UtilityExtensions;
using CmsWeb.Models;

namespace CmsWeb.Areas.OnlineReg.Models
{
    public partial class OnlineRegModel
    {
        public void PrepareMissionTrip(int? gsid, int? goerid)
        {
            if (gsid.HasValue) // this means that the person is a supporter who got a support email
            {
                var goerSupporter = CurrentDatabase.GoerSupporters.SingleOrDefault(gg => gg.Id == gsid); // used for mission trips
                if (goerSupporter != null)
                {
                    GoerId = goerSupporter.GoerId; // support this particular goer
                    Goer = CurrentDatabase.LoadPersonById(goerSupporter.GoerId);
                    GoerSupporterId = gsid;
                }
                else
                {
                    GoerId = 0; // allow this supporter to still select a goer
                }
            }
            else if (goerid.HasValue)
            {
                GoerId = goerid;
                Goer = CurrentDatabase.LoadPersonById(goerid ?? 0);
            }

            // prepare supporter data
            OrganizationMember OrgMember = null;
            if (Goer != null)
            {
                OrgMember = CurrentDatabase.OrganizationMembers.SingleOrDefault(mm => mm.OrganizationId == org.OrganizationId && mm.PeopleId == Goer.PeopleId);
            }
            if (OrgMember != null)
            {
                var supporters = from g in CurrentDatabase.GoerSenderAmounts
                           where g.GoerId == Goer.PeopleId
                           where g.SupporterId != Goer.PeopleId
                           where g.OrgId == org.OrganizationId
                           let anonymous = (from s in CurrentDatabase.GoerSenderAmounts
                                            where s.Id == g.Id
                                            where s.NoNoticeToGoer == true
                                            select s.NoNoticeToGoer).Any()
                           select new Supporter
                           {
                               Id = (anonymous ? 0 : g.SupporterId),    // group all anonymous transactions together even if they're different supporters
                               Name = (anonymous ? "Anonymous" : g.Sender.Name),
                               Amt = (g.Amount ?? 0)
                           };
                var transactions = new TransactionsModel(CurrentDatabase, OrgMember.TranId) { GoerId = Goer.PeopleId };
                var summaries = CurrentDatabase.ViewTransactionSummaries.SingleOrDefault(ts => ts.RegId == OrgMember.TranId && ts.PeopleId == Goer.PeopleId && ts.OrganizationId == org.OrganizationId);
                Supporters = supporters      // combine and total multiple gifts from the same supporter id
                    .GroupBy(s => s.Id)
                    .Select(s => new Supporter { Id = s.First().Id, Name = s.First().Name, TotalAmt = s.Sum(x => x.Amt) })
                    .ToList();
                // prepare funding data
                MissionTripCost = summaries.IndPaid + summaries.IndDue;
                MissionTripRaised = OrgMember.AmountPaidTransactions(CurrentDatabase);
            }

            // prepare date data
            if (org.FirstMeetingDate.HasValue && org.LastMeetingDate.HasValue)
            {
                DateTimeRangeFormatter formatter = new DateTimeRangeFormatter();
                MissionTripDates = formatter.FormatDateRange(org.FirstMeetingDate.Value, org.LastMeetingDate.Value);
            }
            else if (org.FirstMeetingDate.HasValue)
            {
                MissionTripDates = org.FirstMeetingDate.Value.ToString("MMMM d, yyyy");
            }
            else if (org.LastMeetingDate.HasValue)
            {
                MissionTripDates = org.LastMeetingDate.Value.ToString("MMMM d, yyyy");
            }
        }

        public int CheckRegisterLink(string regtag)
        {
            var pid = 0;
            if (regtag.HasValue())
            {
                var guid = regtag.ToGuid();
                if (guid == null)
                {
                    throw new Exception("invalid link");
                }

                var ot = CurrentDatabase.OneTimeLinks.SingleOrDefault(oo => oo.Id == guid.Value);
                if (ot == null)
                {
                    throw new Exception("invalid link");
                }
                if (!Util.IsDebug())
                {
                    if (ot.Used)
                        throw new Exception("link used");
                    if (ot.Expires.HasValue && ot.Expires < DateTime.Now)
                        throw new Exception("link expired");
                }

                registertag = regtag;
                if (registertag.HasValue() && !registerLinkType.HasValue())
                {
                    registerLinkType = "registerlink";
                }

                var a = ot.Querystring.Split(',');
                if (a.Length >= 4)
                {
                    registerLinkType = a[3];
                }

                pid = a[1].ToInt();
            }

            // handle if they are already logged in
            else if (HttpContextFactory.Current.User.Identity.IsAuthenticated)
            {
                pid = CurrentDatabase.UserPeopleId ?? 0;
            }

            if (pid > 0)
            {
                UserPeopleId = pid;
                //                CurrentDatabase.UserPeopleId = pid;
            }
            return pid;
        }

        public class Supporter
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            public decimal Amt { get; set; }
            public decimal TotalAmt { get; set; }
        }
    }
}
