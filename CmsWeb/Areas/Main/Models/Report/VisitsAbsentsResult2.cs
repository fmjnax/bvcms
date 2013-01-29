/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Linq;
using System.Data.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using CmsData;
using UtilityExtensions;
using System.Text;
using System.Web.Mvc;
using System.Collections.Generic;
using CmsWeb.Models;
using CmsData.Codes;

namespace CmsWeb.Areas.Main.Models.Report
{
    public class VisitsAbsentsResult2 : ActionResult
    {
        public class AttendInfo
        {
            public int PeopleId { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Address2 { get; set; }
            public string Email { get; set; }
            public string Birthday { get; set; }
            public string HomePhone { get; set; }
            public string CellPhone { get; set; }
            public string CSZ { get; set; }
            public string Status { get; set; }
            public DateTime? LastAttend { get; set; }
            public decimal? AttendPct { get; set; }
            public string AttendStr { get; set; }
            public bool visitor { get; set; }
            public string MemberStatus { get; set; }
        }
        private Font monofont = FontFactory.GetFont(FontFactory.COURIER, 8);
        private Font boldfont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        private Font bigboldfont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
        private Font font = FontFactory.GetFont(FontFactory.HELVETICA, 10);
        private Font smallfont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new GrayColor(50));
        private PageEvent pageEvents = new PageEvent();
        private PdfPTable t;
        private Document doc;
        private DateTime dt;

        private int? mtgid;
        public VisitsAbsentsResult2(int? id)
        {
            mtgid = id;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var Response = context.HttpContext.Response;
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "filename=foo.pdf");

            dt = Util.Now;

            doc = new Document(PageSize.LETTER.Rotate(), 36, 36, 64, 64);
            var w = PdfWriter.GetInstance(doc, Response.OutputStream);

            var i = (from m in DbUtil.Db.Meetings
                     where m.MeetingId == mtgid
                     select new
                     {
                         m.Organization.OrganizationName,
                         m.Organization.LeaderName,
                         m.MeetingDate
                     }).SingleOrDefault();

            w.PageEvent = pageEvents;
            doc.Open();

            var q = VisitsAbsents(mtgid.Value);

            if (!mtgid.HasValue || i == null || q.Count() == 0)
                doc.Add(new Paragraph("no data"));
            else
            {
                StartPageSet("Visits/Absents Contacts Report: {0} - {1} {2:g}".Fmt(
                    i.OrganizationName, i.LeaderName, i.MeetingDate));

                foreach (var p in q)
                    AddRow(p);
                if (t.Rows.Count > 1)
                    doc.Add(t);
                else
                    doc.Add(new Phrase("no data"));
                pageEvents.EndPageSet();

            }
            doc.Close();
        }

        float[] w = new float[] { 40 + 70 + 80, 40 + 130 };
        float[] w2 = new float[] { 40, 70, 80 };
        float[] w3 = new float[] { 40, 130 };
        int border = PdfPCell.NO_BORDER; //PdfPCell.BOX;

        private void StartPageSet(string header)
        {
            t = new PdfPTable(w);
            t.WidthPercentage = 100;
            t.DefaultCell.Border = border;
            t.DefaultCell.Padding = 5;
            t.HeaderRows = 1;
            pageEvents.StartPageSet(header);

            var t2 = new PdfPTable(w2);
            t2.WidthPercentage = 100;
            t2.DefaultCell.Border = border;
            t2.DefaultCell.Padding = 5;
            t2.AddCell(new Phrase("\nPerson", boldfont));
            t2.AddCell(new Phrase("\nAddress", boldfont));
            t2.AddCell(new Phrase("Phone\nEmail", boldfont));
            var c = new PdfPCell(t.DefaultCell);
            c.AddElement(t2);
            c.Padding = 0;
            t.AddCell(c);

            var t3 = new PdfPTable(w3);
            t3.WidthPercentage = 100;
            t3.DefaultCell.Border = border;
            t3.DefaultCell.Padding = 5;
            t3.DefaultCell.PaddingBottom = 0;
            t3.AddCell(new Phrase("\nBirthday", boldfont));
            t3.AddCell(new Phrase("\nMember Status", boldfont));
            c = new PdfPCell(t.DefaultCell);
            c.Padding = 0;
            c.AddElement(t3);
            t.AddCell(c);
        }
        private void AddRow(AttendInfo p)
        {
            if (t.Rows.Count % 2 == 0)
                t.DefaultCell.BackgroundColor = new GrayColor(240);
            else
                t.DefaultCell.BackgroundColor = BaseColor.WHITE;

            var t2 = new PdfPTable(w2);
            t2.WidthPercentage = 100;
            t2.DefaultCell.Border = border;
            t2.DefaultCell.Padding = 5;
            var name = new Phrase(p.Name + "\n", font);
            name.Add(new Chunk("  ({0})".Fmt(p.PeopleId), smallfont));
            t2.AddCell(name);
            var addr = new StringBuilder(p.Address);
            AddLine(addr, p.Address2);
            AddLine(addr, p.CSZ);
            t2.AddCell(new Phrase(addr.ToString(), font));
            var phones = new StringBuilder();
            AddPhone(phones, p.HomePhone, "h ");
            AddPhone(phones, p.CellPhone, "c ");
            AddLine(phones, p.Email);
            t2.AddCell(new Phrase(phones.ToString(), font));
            var c = new PdfPCell(t.DefaultCell);
            c.AddElement(GetAttendance(p.PeopleId));
            c.Colspan = 3;
            t2.AddCell(c);
            c = new PdfPCell(t.DefaultCell);
            c.Padding = 0;
            c.AddElement(t2);
            t.AddCell(c);

            var t3 = new PdfPTable(w3);
            t3.WidthPercentage = 100;
            t3.DefaultCell.Border = border;
            t3.DefaultCell.Padding = 5;
            t3.DefaultCell.PaddingBottom = 0;
            t3.AddCell(new Phrase(p.Birthday, font));
            t3.AddCell(new Phrase(p.MemberStatus));
            var contacts = GetContacts(p.PeopleId);
            if (contacts.Items.Count > 0)
            {
                c = new PdfPCell(t.DefaultCell);
                c.Colspan = 2;
                c.AddElement(new Chunk("Contacts", boldfont));
                t3.AddCell(c);
                c = new PdfPCell(t.DefaultCell);
                c.Colspan = 2;
                c.AddElement(contacts);
                t3.AddCell(c);
            }
            c = new PdfPCell(t.DefaultCell);
            c.Padding = 0;
            c.AddElement(t3);
            t.AddCell(c);
        }
        private void AddLine(StringBuilder sb, string value)
        {
            AddLine(sb, value, String.Empty);
        }
        private void AddLine(StringBuilder sb, string value, string postfix)
        {
            if (value.HasValue())
            {
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(value);
                if (postfix.HasValue())
                    sb.Append(postfix);
            }
        }
        private void AddPhone(StringBuilder sb, string value, string prefix)
        {
            if (value.HasValue())
            {
                value = value.FmtFone(prefix);
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(value);
            }
        }

        private Paragraph GetAttendance(int pid)
        {
            var q = from a in DbUtil.Db.Attends
                    where a.PeopleId == pid
                    where a.AttendanceFlag == true
                    group a by a.MeetingDate.Date into g
                    orderby g.Key descending
                    select g.Key;
            var list = q.ToList();

            var attstr = new StringBuilder("\n");
            var dt = Util.Now;
            var yearago = dt.AddYears(-1);
            while (dt > yearago)
            {
                var dt2 = dt.AddDays(-7);
                var indicator = ".";
                foreach (var d in list)
                {
                    if (d < dt2)
                        break;
                    if (d <= dt)
                    {
                        indicator = "P";
                        break;
                    }
                }
                attstr.Insert(0, indicator);
                dt = dt2;
            }
            var ph = new Paragraph(attstr.ToString(), monofont);
            ph.SetLeading(0, 1.2f);

            attstr = new StringBuilder();
            foreach (var d in list.Take(8))
                attstr.Insert(0, "{0:d}  ".Fmt(d));
            if (list.Count > 8)
            {
                attstr.Insert(0, "...  ");
                var q2 = q.OrderBy(d => d).Take(Math.Min(list.Count - 8, 3));
                foreach (var d in q2.OrderByDescending(d => d))
                    attstr.Insert(0, "{0:d}  ".Fmt(d));
            }
            ph.Add(new Chunk(attstr.ToString(), smallfont));
            return ph;
        }
        private List GetContacts(int pid)
        {
            var ctl = new CodeValueModel();
            var cts = ctl.ContactTypeCodes();

            var cq = from ce in DbUtil.Db.Contactees
                     where ce.PeopleId == pid
                     orderby ce.contact.ContactDate descending
                     select new
                     {
                         contact = ce.contact,
                         madeby = ce.contact.contactsMakers.FirstOrDefault().person,
                     };
            var list = new iTextSharp.text.List(false, 10);
            list.ListSymbol = new Chunk("\u2022", font);
            var epip = (from p in DbUtil.Db.People
                       where p.PeopleId == pid
                       select new
                       { 
                           ep = p.EntryPoint != null ? p.EntryPoint.Description : "",
                           ip = p.InterestPoint != null ? p.InterestPoint.Description : ""
                       }).Single();
            if (epip.ep.HasValue() || epip.ip.HasValue())
                list.Add(new ListItem(1.2f * font.Size, "Entry, Interest: {0}, {1}".Fmt(epip.ep, epip.ip), font));
            foreach (var pc in cq.Take(10))
            {
                var cname = "unknown";
                if (pc.madeby != null)
                    cname = pc.madeby.Name;
                string ctype = cts.Single(ct => ct.Id == pc.contact.ContactTypeId).Value;
                string comments = null;
                if (pc.contact.Comments.HasValue())
                    comments = pc.contact.Comments.Replace("\r\n\r\n", "\r\n");
                string s = "{0:d}: {1} by {2}\n{3}".Fmt(
                        pc.contact.ContactDate, ctype, cname, comments);
                list.Add(new iTextSharp.text.ListItem(1.2f * font.Size, s, font));
            } 
            if (cq.Count() > 10)
                list.Add(new ListItem(1.2f * font.Size, "(showing most recent 10 of {0})".Fmt(cq.Count()), font));

            return list;
        }
        public IEnumerable<AttendInfo> VisitsAbsents(int mtgid)
        {
            var visitors = new int[] 
            { 
                AttendTypeCode.VisitingMember, 
                AttendTypeCode.RecentVisitor, 
                AttendTypeCode.NewVisitor 
            };
            var q = from a in DbUtil.Db.Attends
                    where a.MeetingId == mtgid
                    where (a.EffAttendFlag == true && visitors.Contains(a.AttendanceTypeId.Value))
                        || a.EffAttendFlag == false
                    let p = a.Person
                    let status = a.EffAttendFlag == false ? a.MemberType.Description : a.AttendType.Description
                    let lastattend = a.Organization.Attends
                                    .Where(aa => aa.PeopleId == a.PeopleId && aa.AttendanceFlag == true)
                                    .Where(aa => aa.MeetingId != mtgid)
                                    .Max(aa => aa.MeetingDate)
                    let attendpct = a.Organization.OrganizationMembers
                                    .Where(aa => aa.PeopleId == a.PeopleId)
                                    .Select(aa => aa.AttendPct)
                                    .SingleOrDefault()
                    let attendstr = a.Organization.OrganizationMembers
                                    .Where(aa => aa.PeopleId == a.PeopleId)
                                    .Select(aa => aa.AttendStr)
                                    .SingleOrDefault()
                    orderby a.EffAttendFlag descending, a.Person.Name2
                    select new AttendInfo
                    {
                        PeopleId = p.PeopleId,
                        Name = p.Name,
                        Address = p.PrimaryAddress,
                        Birthday = p.DOB.ToDate().ToString2("m"),
                        Email = p.EmailAddress,
                        HomePhone = p.HomePhone,
                        CellPhone = p.CellPhone,
                        CSZ = Util.FormatCSZ4(p.PrimaryCity, p.PrimaryState, p.PrimaryZip),
                        Status = status,
                        LastAttend = lastattend,
                        AttendPct = attendpct,
                        AttendStr = attendstr,
                        visitor = a.EffAttendFlag == true,
                        MemberStatus = a.Person.MemberStatus.Description
                    };
            return q;
        }
        class PageEvent : PdfPageEventHelper
        {
            private PdfTemplate npages;
            private PdfWriter writer;
            private Document document;
            private PdfContentByte dc;
            private BaseFont font;
            private string HeadText;

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                this.writer = writer;
                this.document = document;
                base.OnOpenDocument(writer, document);
                font = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                dc = writer.DirectContent;
            }
            public void EndPageSet()
            {
                if (npages == null)
                    return;
                npages.BeginText();
                npages.SetFontAndSize(font, 8);
                npages.ShowText((writer.PageNumber + 1).ToString());
                npages.EndText();
            }
            public void StartPageSet(string header1)
            {
                EndPageSet();
                document.NewPage();
                document.ResetPageCount();
                this.HeadText = header1;
                npages = dc.CreateTemplate(50, 50);
            }
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                base.OnEndPage(writer, document);

                string text;
                float len;

                //---Header left
                text = HeadText;
                const float headFontSize = 11f;
                len = font.GetWidthPoint(text, headFontSize);
                dc.BeginText();
                dc.SetFontAndSize(font, headFontSize);
                dc.SetTextMatrix(30, document.PageSize.Height - 30);
                dc.ShowText(text);
                dc.EndText();

                //---Column 1
                text = "Page " + (writer.PageNumber + 1) + " of ";
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(30, 30);
                dc.ShowText(text);
                dc.EndText();
                dc.AddTemplate(npages, 30 + len, 30);

                //---Column 2
                text = HeadText;
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(document.PageSize.Width / 2 - len / 2, 30);
                dc.ShowText(text);
                dc.EndText();

                //---Column 3
                text = Util.Now.ToShortDateString();
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(document.PageSize.Width - 30 - len, 30);
                dc.ShowText(text);
                dc.EndText();
            }
        }
    }
}