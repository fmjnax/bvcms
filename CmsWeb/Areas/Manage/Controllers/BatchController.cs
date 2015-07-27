using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Mvc;
using CmsData;
using CmsWeb.Code;
using CmsWeb.Models;
using LumenWorks.Framework.IO.Csv;
using UtilityExtensions;
using Alias = System.Threading.Tasks;

namespace CmsWeb.Areas.Manage.Controllers
{
    [ValidateInput(false)]
    [RouteArea("Manage", AreaPrefix = "Batch"), Route("{action}/{id?}")]
    public class BatchController : CmsStaffAsyncController
    {
        [Authorize(Roles = "Admin")]
        public ActionResult MoveAndDelete()
        {
            return View();
        }

        [HttpPost]
        [AsyncTimeout(600000)]
        public void MoveAndDeleteAsync(string text)
        {
            AsyncManager.OutstandingOperations.Increment();
            var host = Util.Host;
            ThreadPool.QueueUserWorkItem(e =>
            {
                var sb = new StringBuilder();
                sb.Append("<h2>done</h2>\n<p><a href='/'>home</a></p>\n");
                using (var csv = new CsvReader(new StringReader(text), false, '\t'))
                {
                    while (csv.ReadNextRecord())
                    {
                        if (csv.FieldCount != 2)
                        {
                            sb.AppendFormat("expected two ids, row {0}<br/>\n", csv[0]);
                            continue;
                        }

                        var fromid = csv[0].ToInt();
                        var toid = csv[1].ToInt();
                        var Db = DbUtil.Create(host);
                        var p = Db.LoadPersonById(fromid);

                        if (p == null)
                        {
                            sb.AppendFormat("fromid {0} not found<br/>\n", fromid);
                            Db.Dispose();
                            continue;
                        }
                        var tp = Db.LoadPersonById(toid);
                        if (tp == null)
                        {
                            sb.AppendFormat("toid {0} not found<br/>\n", toid);
                            Db.Dispose();
                            continue;
                        }
                        try
                        {
                            p.MovePersonStuff(Db, toid);
                            Db.SubmitChanges();
                        }
                        catch (Exception ex)
                        {
                            sb.AppendFormat("error on move ({0}, {1}): {2}<br/>\n", fromid, toid, ex.Message);
                            Db.Dispose();
                            continue;
                        }
                        try
                        {
                            Db.PurgePerson(fromid);
                            sb.AppendFormat("moved ({0}, {1}) successful<br/>\n", fromid, toid);
                        }
                        catch (Exception ex)
                        {
                            sb.AppendFormat("error on delete ({0}): {1}<br/>\n", fromid, ex.Message);
                        }
                        finally
                        {
                            Db.Dispose();
                        }
                    }
                }
                AsyncManager.Parameters["results"] = sb.ToString();
                AsyncManager.OutstandingOperations.Decrement();
            });
        }

        public ActionResult MoveAndDeleteCompleted(string results)
        {
            return Content(results);
        }

        public ActionResult Grade(string text)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
            {
                ViewData["text"] = "";
                return View();
            }
            var batch = from s in text.Split('\n')
                        where s.HasValue()
                        let a = s.SplitStr("\t", 3)
                        select new {pid = a[0].ToInt(), oid = a[1].ToInt(), grade = a[2].ToInt()};
            foreach (var i in batch)
            {
                var m = DbUtil.Db.OrganizationMembers.Single(om => om.OrganizationId == i.oid && om.PeopleId == i.pid);
                m.Grade = i.grade;
            }
            DbUtil.Db.SubmitChanges();

            return Content("done");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult RegistrationMail(string text)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
            {
                ViewData["text"] = "";
                return View();
            }
            var batch = from s in text.Split('\n')
                        where s.HasValue()
                        let a = s.SplitStr("\t", 2)
                        select new {pid = a[0].ToInt(), em = a[1]};
            foreach (var i in batch)
            {
                var m = DbUtil.Db.OrganizationMembers.SingleOrDefault(om => om.OrganizationId == 88485 && om.PeopleId == i.pid);
                if (m == null)
                    continue;
                m.RegisterEmail = i.em;
            }
            DbUtil.Db.SubmitChanges();

            return Content("done");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult UpdateOrg()
        {
            ViewData["text"] = "";
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult UpdateOrg(string text)
        {
            var csv = new CsvReader(new StringReader(text), true, '\t');
            var cols = csv.GetFieldHeaders();

            while (csv.ReadNextRecord())
            {
                var oid = csv[0].ToInt();
                var o = DbUtil.Db.LoadOrganizationById(oid);
                for (var c = 1; c < csv.FieldCount; c++)
                {
                    var val = csv[c].Trim();
                    var name = cols[c].Trim();
                    switch (name)
                    {
                        case "Campus":
                            if (val.AllDigits())
                            {
                                o.CampusId = val.ToInt();
                                if (o.CampusId == 0)
                                    o.CampusId = null;
                            }
                            break;
                        case "CanSelfCheckin":
                            o.CanSelfCheckin = val.ToBool2();
                            break;
                        case "RegStart":
                            o.RegStart = val.ToDate();
                            break;
                        case "RegEnd":
                            o.RegEnd = val.ToDate();
                            break;
                        case "Schedule":
                            if (val.HasValue() && val.NotEqual("None"))
                            {
                                var sc = o.OrgSchedules.FirstOrDefault();
                                var scin = Organization.ParseSchedule(val);
                                if (sc != null)
                                {
                                    sc.SchedDay = scin.SchedDay;
                                    sc.SchedTime = scin.SchedTime;
                                }
                                else
                                    o.OrgSchedules.Add(scin);
                            }
                            if (val.Equal("None"))
                                DbUtil.Db.OrgSchedules.DeleteAllOnSubmit(o.OrgSchedules);
                            break;
                        case "BirthDayStart":
                            o.BirthDayStart = val.ToDate();
                            break;
                        case "BirthDayEnd":
                            o.BirthDayEnd = val.ToDate();
                            break;
                        case "EntryPoint":
                            if (val.AllDigits())
                            {
                                var id = val.ToInt();
                                if (id > 0)
                                    o.EntryPointId = id;
                            }
                            break;
                        case "LeaderType":
                            if (val.AllDigits())
                            {
                                var id = val.ToInt();
                                if (id > 0)
                                    o.LeaderMemberTypeId = id;
                            }
                            break;
                        case "SecurityType":
                            o.SecurityTypeId = val.Equal("LeadersOnly") ? 2 : val.Equal("UnShared") ? 3 : 0;
                            break;
                        case "FirstMeeting":
                            o.FirstMeetingDate = val.ToDate();
                            break;
                        case "Gender":
                            o.GenderId = val.Equal("Male") ? 1 : val.Equal("Female") ? (int?) 2 : null;
                            break;
                        case "GradeAgeStart":
                            o.GradeAgeStart = val.ToInt2();
                            break;
                        case "MainFellowshipOrg":
                            o.IsBibleFellowshipOrg = val.ToBool2();
                            break;
                        case "LastDayBeforeExtra":
                            o.LastDayBeforeExtra = val.ToDate();
                            break;
                        case "LastMeeting":
                            o.LastMeetingDate = val.ToDate();
                            break;
                        case "Limit":
                            o.Limit = val.ToInt2();
                            break;
                        case "Location":
                            o.Location = val;
                            break;
                        case "AppCategory":
                            o.AppCategory = val;
                            break;
                        case "PublicSortOrder":
                            o.PublicSortOrder = val;
                            break;
                        case "UseRegisterLink2":
                            o.UseRegisterLink2 = val.ToBool2();
                            break;
                        case "Name":
                            o.OrganizationName = val;
                            break;
                        case "NoSecurityLabel":
                            o.NoSecurityLabel = val.ToBool2();
                            break;
                        case "NumCheckInLabels":
                            o.NumCheckInLabels = val.ToInt2();
                            break;
                        case "NumWorkerCheckInLabels":
                            o.NumWorkerCheckInLabels = val.ToInt2();
                            break;
                        case "OnLineCatalogSort":
                            o.OnLineCatalogSort = val == "0" ? null : val.ToInt2();
                            break;
                        case "OrganizationStatusId":
                            o.OrganizationStatusId = val.ToInt();
                            break;
                        case "PhoneNumber":
                            o.PhoneNumber = val;
                            break;
                        case "Description":
                            o.Description = val;
                            break;
                        case "RollSheetVisitorWks":
                            o.RollSheetVisitorWks = val == "0" ? null : val.ToInt2();
                            break;

                        default:
                            if (name.EndsWith(".ev"))
                                if (val.HasValue())
                                {
                                    var a = name.Substring(0, name.Length - 3);
                                    o.AddEditExtraData(a, val);
                                }
                            break;
                    }
                    DbUtil.Db.SubmitChanges();
                }
            }
            return Content("Organizations were successfully updated.");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateFields() // UpdateForATag
        {
            var m = new UpdateFieldsModel();
            var success = (string) TempData["success"];
            if (success.HasValue())
                ViewData["success"] = success;
            ViewData["text"] = "";
            return View(m);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateLookupValueSelection(string field)
        {
            IEnumerable<CodeValueItem> m = null;
            var lookups = new CodeValueModel();
            switch (field)
            {
                case "Approval Codes":
                    m = lookups.VolunteerCodes();
                    ViewBag.UseCode = false;
                    break;
                case "Baptism Status":
                    m = lookups.BaptismStatusList();
                    ViewBag.UseCode = false;
                    break;
                case "Baptism Type":
                    m = lookups.BaptismTypeList();
                    ViewBag.UseCode = false;
                    break;
                case "Bad Address Flag":
                    m = UpdateFieldsModel.BadAddressFlag();
                    ViewBag.UseCode = true;
                    break;
                case "Campus":
                    m = lookups.AllCampuses();
                    ViewBag.UseCode = false;
                    break;
                case "Statement Options":
                    m = lookups.EnvelopeOptionList();
                    ViewBag.UseCode = false;
                    break;
                case "Electronic Statement":
                    m = UpdateFieldsModel.ElectronicStatement();
                    ViewBag.UseCode = true;
                    break;
                case "Decision Type":
                    m = lookups.DecisionTypeList();
                    ViewBag.UseCode = false;
                    break;
                case "Do Not Mail":
                    m = UpdateFieldsModel.DoNotMail();
                    ViewBag.UseCode = true;
                    break;
                case "Drop Type":
                    m = lookups.DropTypeList();
                    ViewBag.UseCode = false;
                    break;
                case "Envelope Options":
                    m = lookups.EnvelopeOptionList();
                    ViewBag.UseCode = false;
                    break;
                case "Entry Point":
                    m = lookups.EntryPoints();
                    ViewBag.UseCode = false;
                    break;
                case "Family Position":
                    m = lookups.FamilyPositionCodes();
                    ViewBag.UseCode = false;
                    break;
                case "Gender":
                    m = lookups.GenderCodes();
                    ViewBag.UseCode = false;
                    break;
                case "Grade":
                    m = UpdateFieldsModel.Grades();
                    ViewBag.UseCode = true;
                    break;
                case "Join Type":
                    m = lookups.JoinTypeList();
                    ViewBag.UseCode = false;
                    break;
                case "Marital Status":
                    m = lookups.MaritalStatusCodes();
                    ViewBag.UseCode = false;
                    break;
                case "Member Status":
                    m = lookups.MemberStatusCodes();
                    ViewBag.UseCode = false;
                    break;
                case "New Member Class":
                    m = lookups.NewMemberClassStatusList();
                    ViewBag.UseCode = false;
                    break;
                case "ReceiveSMS":
                    m = UpdateFieldsModel.ReceiveSMS();
                    ViewBag.UseCode = true;
                    break;
            }
            ViewBag.FieldName = field;
            return View(m);
        }

        [HttpPost]
        public ActionResult UpdateFieldsRun(UpdateFieldsModel m)
        {
            m.Run(ModelState);
            if (!ModelState.IsValid)
                return View("UpdateFields", m);

            TempData["success"] = $"{m.Field} updated with the value '{m.NewValue}' for {m.Count} records ";
            return RedirectToAction("UpdateFields");
        }

        [HttpPost]
        public ActionResult UpdateFieldsCount(UpdateFieldsModel m)
        {
            var q = m.People();
            return Content(q.Count().ToString());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult UpdateStatusFlags()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult UpdateStatusFlags(FormCollection formCollection)
        {
            DbUtil.Db.DeleteQueryBitTags();
            var qbits = DbUtil.Db.StatusFlags().ToList();
            foreach (var a in qbits)
            {
                var t = DbUtil.Db.FetchOrCreateSystemTag(a[0]);
                var qq = DbUtil.Db.PeopleQuery2(a[0] + ":" + a[1]);
                if (qq == null)
                    continue;
                DbUtil.Db.TagAll2(qq, t);
            }
            return Content("Status flags were successfully updated.");
        }

        [HttpGet]
        [Authorize(Roles = "Edit")]
        public ActionResult FindTagPeople()
        {
            return View("FindTagPeople0");
        }

        [HttpPost]
        private string FindColumn(Dictionary<string, int> names, string[] a, string col)
        {
            if (names.ContainsKey(col))
                return a[names[col]];
            return null;
        }

        private string FindColumnDigits(Dictionary<string, int> names, string[] a, string col)
        {
            var s = FindColumn(names, a, col);
            if (s.HasValue())
                return s.GetDigits();
            return s;
        }

        private DateTime? FindColumnDate(Dictionary<string, int> names, string[] a, string col)
        {
            var s = FindColumn(names, a, col);
            DateTime dt;
            if (names.ContainsKey(col))
                if (DateTime.TryParse(a[names[col]], out dt))
                    return dt;
            return null;
        }

        [HttpPost]
        [Authorize(Roles = "Edit")]
        public ActionResult FindTagPeople(string text, string tagname)
        {
            if (!tagname.HasValue())
                return Content("no tag");
            var csv = new CsvReader(new StringReader(text), false, '\t').ToList();

            if (!csv.Any())
                return Content("no data");
            var line0 = csv.First().ToList();
            var names = line0.ToDictionary(i => i.TrimEnd(),
                i => line0.FindIndex(s => s == i));
            var ActiveNames = new List<string>
            {
                "First",
                "Last",
                "Birthday",
                "Email",
                "CellPhone",
                "HomePhone"
            };
            var hasvalidcolumn = false;
            foreach (var name in names.Keys)
                if (ActiveNames.Contains(name))
                {
                    hasvalidcolumn = true;
                    break;
                }
            if (!hasvalidcolumn)
                return Content("no valid column");


            var list = new List<FindInfo>();
            foreach (var a in csv.Skip(1))
            {
                var row = new FindInfo();
                row.First = FindColumn(names, a, "First");
                row.Last = FindColumn(names, a, "Last");
                row.Birthday = FindColumnDate(names, a, "Birthday");
                row.Email = FindColumn(names, a, "Email");
                row.CellPhone = FindColumnDigits(names, a, "CellPhone");
                row.HomePhone = FindColumnDigits(names, a, "HomePhone");

                var pids = DbUtil.Db.FindPerson3(row.First, row.Last, row.Birthday, row.Email, row.CellPhone, row.HomePhone, null);
                row.Found = pids.Count();
                if (row.Found == 1)
                    row.PeopleId = pids.Single().PeopleId.Value;
                list.Add(row);
            }
            var q = from pi in list
                    where pi.PeopleId.HasValue
                    select pi.PeopleId;
            foreach (var pid in q.Distinct())
                Person.Tag(DbUtil.Db, pid.Value, tagname, Util.UserPeopleId, DbUtil.TagTypeId_Personal);
            DbUtil.Db.SubmitChanges();

            return View(list);
        }

        [Authorize(Roles = "Edit")]
        public ActionResult FindTagEmail(string emails, string tagname)
        {
            if (Request.HttpMethod.ToUpper() == "GET")
                return View();
            if (!tagname.HasValue())
                return Content("no tag");

            var a = emails.SplitLines();
            var q = from p in DbUtil.Db.People
                    where a.Contains(p.EmailAddress) || a.Contains(p.EmailAddress2)
                    select p.PeopleId;
            foreach (var pid in q.Distinct())
                Person.Tag(DbUtil.Db, pid, tagname, Util.UserPeopleId, DbUtil.TagTypeId_Personal);
            DbUtil.Db.SubmitChanges();
            return Redirect("/Tags?tag=" + tagname);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        [Authorize(Roles = "Edit")]
        public ActionResult TagPeopleIds()
        {
            return View();
        }

        public ActionResult TagUploadPeopleIds(string name, string text, bool newtag)
        {
            var q = from line in text.Split('\n')
                    select line.ToInt();
            if (newtag)
            {
                var tag = DbUtil.Db.FetchTag(name, Util.UserPeopleId, DbUtil.TagTypeId_Personal);
                if (tag != null)
                    DbUtil.Db.ExecuteCommand("delete TagPerson where Id = {0}", tag.Id);
            }
            foreach (var pid in q)
            {
                Person.Tag(DbUtil.Db, pid, name, DbUtil.Db.CurrentUser.PeopleId, DbUtil.TagTypeId_Personal);
                DbUtil.Db.SubmitChanges();
            }
            return Redirect("/Tags?tag=" + name);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult ExtraValuesFromPeopleIds()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ExtraValuesFromPeopleIds(string text, string field)
        {
            var csv = new CsvReader(new StringReader(text), false, '\t').ToList();
            foreach (var a in csv)
            {
                var p = DbUtil.Db.LoadPersonById(a[0].ToInt());
                p.AddEditExtraValue(field, a[1]);
                DbUtil.Db.SubmitChanges();
            }
            return Redirect("/ExtraValue/Summary/People");
        }

        [HttpGet]
        public ActionResult TestScript()
        {
#if DEBUG
            ViewBag.Script =
                @"
model.TestEmail = True
model.EmailContent(
    'RecentMovedOutOfTown',
    819918, 'karen@touchpointsoftware.com', 'Karen Worrell',
    'RecentMovedOutOfTownMessage')
model.AddExtraValueDate( 'RecentMovedOutOfTown',  'RecentMoveNotified',  model.DateTime )
";
#endif
            return View();
        }

        [HttpPost]
        public ActionResult RunTestScript(string script)
        {
            Util.IsInRoleEmailTest = true;
            var ret = PythonEvents.RunScript(Util.Host, script);
            return Content(ret);
        }

        [HttpGet]
        [Authorize(Roles = "Finance")]
        public ActionResult DoGiving()
        {
            ManagedGiving.DoAllGiving(DbUtil.Db);
            return Content("done");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult SQLView(string id)
        {
            try
            {
                var cmd = new SqlCommand("select * from guest." + id.Replace(" ", ""));
                cmd.Connection = new SqlConnection(Util.ConnectionString);
                cmd.Connection.Open();
                var rdr = cmd.ExecuteReader();
                return View(rdr);
            }
            catch (Exception)
            {
                return Content("cannot find view guest." + id);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult RunScript(string id)
        {
            return Redirect("/PyScript/" + id);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Script(string id)
        {
            try
            {
                var script = DbUtil.Db.Content(id);
                PythonEvents.RunScript(Util.Host, script.Body);
            }
            catch (Exception e)
            {
                return Content(e.Message);
            }
            return Content("done");
        }

        [HttpGet]
        [Authorize(Roles = "Developer")]
        public ActionResult OtherDeveloperActions()
        {
            return View();
        }

        public class FindInfo
        {
            public int? PeopleId { get; set; }
            public int Found { get; set; }
            public string First { get; set; }
            public string Last { get; set; }
            public string Email { get; set; }
            public string CellPhone { get; set; }
            public string HomePhone { get; set; }
            public DateTime? Birthday { get; set; }
        }
    }
}
