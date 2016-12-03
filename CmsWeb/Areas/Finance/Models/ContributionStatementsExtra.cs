/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CmsData;
using CmsData.API;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using UtilityExtensions;

namespace CmsWeb.Areas.Finance.Models.Report
{
    /*  In ContributionStatements.cs file
    public class MyHandler : IElementHandler
    {
         public List<IElement> elements = new List<IElement>();

         public void Add(IWritable w)
         {
              if (w is WritableElement)
              {
                    elements.AddRange(((WritableElement)w).Elements());
              }
         }
    }
    */

    public class ContributionStatementsExtra
    {
        private readonly PageEvent pageEvents = new PageEvent();
        public int FamilyId { get; set; }
        public int PeopleId { get; set; }
        public int? SpouseId { get; set; }
        public int typ { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool ShowCheckNo { get; set; }
        public bool ShowNotes { get; set; }

        public int LastSet()
        {
            if (pageEvents.FamilySet.Count == 0)
                return 0;
            var m = pageEvents.FamilySet.Max(kp => kp.Value);
            return m;
        }

        public List<int> Sets()
        {
            if (pageEvents.FamilySet.Count == 0)
                return new List<int>();
            var m = pageEvents.FamilySet.Values.Distinct().ToList();
            return m;
        }

        public void Run(Stream stream, CMSDataContext Db, IEnumerable<ContributorInfo> q, int set = 0)
        {
            pageEvents.set = set;
            pageEvents.PeopleId = 0;
            var contributors = q;

            PdfContentByte dc;
            var font = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            var boldfont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);

            var doc = new Document(PageSize.LETTER);
            doc.SetMargins(36f, 30f, 24f, 36f);
            var w = PdfWriter.GetInstance(doc, stream);
            w.PageEvent = pageEvents;
            doc.Open();
            dc = w.DirectContent;

            var prevfid = 0;
            var runningtotals = Db.ContributionsRuns.OrderByDescending(mm => mm.Id).FirstOrDefault();
            runningtotals.Processed = 0;
            Db.SubmitChanges();
            var count = 0;
            foreach (var ci in contributors)
            {

                if (set > 0 && pageEvents.FamilySet[ci.PeopleId] != set)
                    continue;

                var contributions = APIContribution.contributions(Db, ci, FromDate, ToDate).ToList();
                var pledges = APIContribution.pledges(Db, ci, ToDate).ToList();
                var giftsinkind = APIContribution.GiftsInKind(Db, ci, FromDate, ToDate).ToList();
                var nontaxitems = Db.Setting("DisplayNonTaxOnStatement", "false").ToBool()
                    ? APIContribution.NonTaxItems(Db, ci, FromDate, ToDate).ToList()
                    : new List<ContributionInfo>();

                if ((contributions.Count + pledges.Count + giftsinkind.Count + nontaxitems.Count) == 0)
                {
                    runningtotals.Processed += 1;
                    runningtotals.CurrSet = set;
                    Db.SubmitChanges();
                    if (set == 0)
                        pageEvents.FamilySet[ci.PeopleId] = 0;
                    continue;
                }

                pageEvents.NextPeopleId = ci.PeopleId;
                doc.NewPage();
                if (prevfid != ci.FamilyId)
                {
                    prevfid = ci.FamilyId;
                    pageEvents.EndPageSet();
                    pageEvents.PeopleId = ci.PeopleId;
                }
                if (set == 0)
                    pageEvents.FamilySet[ci.PeopleId] = 0;
                count++;

                var css = @"
<style>
h1 { font-size: 24px; font-weight:normal; margin-bottom:0; }
h2 { font-size: 11px; font-weight:normal; margin-top: 0; }
p { font-size: 11px; }
</style>
";
                //----Church Name

                var t1 = new PdfPTable(1);
                t1.TotalWidth = 72f*5f;
                t1.DefaultCell.Border = Rectangle.NO_BORDER;
                var html1 = Db.ContentHtml("StatementHeader", Resource1.ContributionStatementHeader);
                var html2 = Db.ContentHtml("StatementNotice", Resource1.ContributionStatementNotice);

                var mh = new MyHandler();
                using (var sr = new StringReader(css + html1))
                    XMLWorkerHelper.GetInstance().ParseXHtml(mh, sr);

                var cell = new PdfPCell(t1.DefaultCell);
                foreach (var e in mh.elements)
                    if (e.Chunks.Count > 0)
                        cell.AddElement(e);
                //cell.FixedHeight = 72f * 1.25f;
                t1.AddCell(cell);
                t1.AddCell("\n");

                var t1a = new PdfPTable(1);
                t1a.TotalWidth = 72f*5f;
                t1a.DefaultCell.Border = Rectangle.NO_BORDER;

                var ae = new PdfPTable(1);
                ae.DefaultCell.Border = Rectangle.NO_BORDER;
                ae.WidthPercentage = 100;

                var a = new PdfPTable(1);
                a.DefaultCell.Indent = 25f;
                a.DefaultCell.Border = Rectangle.NO_BORDER;
                a.AddCell(new Phrase(ci.Name, font));
                foreach (var line in ci.MailingAddress.SplitLines())
                    a.AddCell(new Phrase(line, font));
                cell = new PdfPCell(a) {Border = Rectangle.NO_BORDER};
                //cell.FixedHeight = 72f * 1.0625f;
                ae.AddCell(cell);

                cell = new PdfPCell(t1a.DefaultCell);
                cell.AddElement(ae);
                t1a.AddCell(ae);

                //-----Notice

                var t2 = new PdfPTable(1);
                t2.TotalWidth = 72f*3f;
                t2.DefaultCell.Border = Rectangle.NO_BORDER;
                t2.AddCell(Db.Setting("NoPrintDateOnStatement")
                    ? new Phrase($"\nID:{ci.PeopleId} {ci.CampusId}", font) 
                    : new Phrase($"\nPrint Date: {DateTime.Now:d}   (id:{ci.PeopleId} {ci.CampusId})", font));
                t2.AddCell("");
                var mh2 = new MyHandler();
                using (var sr = new StringReader(css + html2))
                    XMLWorkerHelper.GetInstance().ParseXHtml(mh2, sr);
                cell = new PdfPCell(t1.DefaultCell);
                foreach (var e in mh2.elements)
                    if (e.Chunks.Count > 0)
                        cell.AddElement(e);
                t2.AddCell(cell);

                // POSITIONING OF ADDRESSES
                //----Header

                var yp = doc.BottomMargin +
                         Db.Setting("StatementRetAddrPos", "10.125").ToFloat()*72f;
                t1.WriteSelectedRows(0, -1,
                    doc.LeftMargin - 0.1875f*72f, yp, dc);

                yp = doc.BottomMargin +
                     Db.Setting("StatementAddrPos", "8.3375").ToFloat()*72f;
                t1a.WriteSelectedRows(0, -1, doc.LeftMargin, yp, dc);

                yp = doc.BottomMargin + 10.125f*72f;
                t2.WriteSelectedRows(0, -1, doc.LeftMargin + 72f*4.4f, yp, dc);

                //----Contributions

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" ") {SpacingBefore = 72f*2.125f});

                doc.Add(new Phrase($"\n  Period: {FromDate:d} - {ToDate:d}", boldfont));

                var pos = w.GetVerticalPosition(true);

                var ct = new ColumnText(dc);
                var colwidth = (doc.Right - doc.Left);

                var t = new PdfPTable(new[] {15f, 25f, 15f, 15f, 30f});
                t.WidthPercentage = 100;
                t.DefaultCell.Border = Rectangle.NO_BORDER;
                t.HeaderRows = 2;

                cell = new PdfPCell(t.DefaultCell);
                cell.Colspan = 5;
                cell.Phrase = new Phrase("Contributions\n", boldfont);
                t.AddCell(cell);

                t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                t.AddCell(new Phrase("Date", boldfont));
                t.AddCell(new Phrase("Description", boldfont));

                cell = new PdfPCell(t.DefaultCell);
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Phrase = new Phrase("Amount", boldfont);
                t.AddCell(cell);

                cell = new PdfPCell(t.DefaultCell);
                cell.HorizontalAlignment = Element.ALIGN_CENTER;

                if (ShowCheckNo)
                    cell.Phrase = new Phrase("Check No", boldfont);
                else
                    cell.Phrase = new Phrase("", boldfont);

                t.AddCell(cell);

                if (ShowNotes)
                    t.AddCell(new Phrase("Notes", boldfont));
                else
                    t.AddCell(new Phrase("", boldfont));

                t.DefaultCell.Border = Rectangle.NO_BORDER;

                var total = 0m;
                foreach (var c in contributions)
                {
                    t.AddCell(new Phrase(c.ContributionDate.ToShortDateString(), font));
                    t.AddCell(new Phrase(c.Fund, font));

                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Phrase = new Phrase(c.ContributionAmount.ToString("N2"), font);
                    t.AddCell(cell);

                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;

                    if (ShowCheckNo)
                        cell.Phrase = new Phrase(c.CheckNo, font);
                    else
                        cell.Phrase = new Phrase("", font);

                    t.AddCell(cell);

                    if (ShowNotes)
                        t.AddCell(new Phrase(c.Description, font));
                    else
                        t.AddCell(new Phrase("", font));

                    total += (c.ContributionAmount);
                }

                t.DefaultCell.Border = Rectangle.TOP_BORDER;

                cell = new PdfPCell(t.DefaultCell);
                cell.Colspan = 2;
                cell.Phrase = new Phrase("Total Contributions for period", boldfont);
                t.AddCell(cell);

                cell = new PdfPCell(t.DefaultCell);
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Phrase = new Phrase(total.ToString("N2"), font);
                t.AddCell(cell);

                cell = new PdfPCell(t.DefaultCell);
                cell.Colspan = 2;
                cell.Phrase = new Phrase("");
                t.AddCell(cell);

                ct.AddElement(t);

                //------Pledges

                if (pledges.Count > 0)
                {
                    t = new PdfPTable(new[] {25f, 15f, 15f, 15f, 30f});
                    t.WidthPercentage = 100;
                    t.DefaultCell.Border = Rectangle.NO_BORDER;
                    t.HeaderRows = 2;

                    cell = new PdfPCell(t.DefaultCell);
                    cell.Colspan = 5;
                    cell.Phrase = new Phrase("\n\nPledges\n", boldfont);
                    t.AddCell(cell);

                    t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                    t.AddCell(new Phrase("Fund", boldfont));
                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Phrase = new Phrase("Pledge", boldfont);
                    t.AddCell(cell);
                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Phrase = new Phrase("Given", boldfont);
                    t.AddCell(cell);

                    t.DefaultCell.Border = Rectangle.NO_BORDER;
                    t.AddCell(new Phrase("", boldfont));
                    t.AddCell(new Phrase("", boldfont));

                    foreach (var c in pledges)
                    {
                        t.AddCell(new Phrase(c.Fund, font));

                        cell = new PdfPCell(t.DefaultCell);
                        cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell.Phrase = new Phrase(c.PledgeAmount.ToString2("N2"), font);
                        t.AddCell(cell);

                        cell = new PdfPCell(t.DefaultCell);
                        cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell.Phrase = new Phrase(c.ContributionAmount.ToString2("N2"), font);
                        t.AddCell(cell);

                        t.AddCell(new Phrase("", boldfont));
                        t.AddCell(new Phrase("", boldfont));
                    }
                    ct.AddElement(t);
                }

                //------Gifts In Kind

                if (giftsinkind.Count > 0)
                {
                    t = new PdfPTable(new[] {15f, 25f, 15f, 15f, 30f});
                    t.WidthPercentage = 100;
                    t.DefaultCell.Border = Rectangle.NO_BORDER;
                    t.HeaderRows = 2;

                    // Headers
                    cell = new PdfPCell(t.DefaultCell);
                    cell.Colspan = 5;
                    cell.Phrase = new Phrase("\n\nGifts in Kind\n", boldfont);
                    t.AddCell(cell);

                    t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                    t.AddCell(new Phrase("Date", boldfont));
                    cell = new PdfPCell(t.DefaultCell);
                    cell.Phrase = new Phrase("Fund", boldfont);
                    t.AddCell(cell);
                    cell = new PdfPCell(t.DefaultCell);
                    cell.Phrase = new Phrase("Description", boldfont);
                    t.AddCell(cell);

                    t.AddCell(new Phrase("", boldfont));
                    t.AddCell(new Phrase("", boldfont));

                    t.DefaultCell.Border = Rectangle.NO_BORDER;

                    foreach (var c in giftsinkind)
                    {
                        t.AddCell(new Phrase(c.ContributionDate.ToShortDateString(), font));
                        cell = new PdfPCell(t.DefaultCell);

                        cell.Phrase = new Phrase(c.Fund, font);
                        t.AddCell(cell);

                        cell = new PdfPCell(t.DefaultCell);
                        cell.Colspan = 3;
                        cell.Phrase = new Phrase(c.Description, font);
                        t.AddCell(cell);
                    }
                    ct.AddElement(t);
                }

                //-----Summary

                t = new PdfPTable(new[] {40f, 15f, 45f});
                t.WidthPercentage = 100;
                t.DefaultCell.Border = Rectangle.NO_BORDER;
                t.HeaderRows = 2;

                cell = new PdfPCell(t.DefaultCell);
                cell.Colspan = 3;
                cell.Phrase = new Phrase("\n\nPeriod Summary\n", boldfont);
                t.AddCell(cell);

                t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                t.AddCell(new Phrase("Fund", boldfont));

                cell = new PdfPCell(t.DefaultCell);
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Phrase = new Phrase("Amount", boldfont);
                t.AddCell(cell);

                t.DefaultCell.Border = Rectangle.NO_BORDER;
                t.AddCell(new Phrase("", boldfont));

                foreach (var c in APIContribution.quarterlySummary(Db, ci, FromDate, ToDate))
                {
                    t.AddCell(new Phrase(c.Fund, font));

                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Phrase = new Phrase(c.ContributionAmount.ToString("N2"), font);
                    t.AddCell(cell);

                    t.AddCell(new Phrase("", boldfont));
                }

                t.DefaultCell.Border = Rectangle.NO_BORDER;

                cell = new PdfPCell(t.DefaultCell);
                cell.Border = Rectangle.TOP_BORDER;
                cell.Colspan = 1;
                cell.Phrase = new Phrase("Total Contributions for period", boldfont);
                t.AddCell(cell);

                cell = new PdfPCell(t.DefaultCell);
                cell.Border = Rectangle.TOP_BORDER;
                cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                cell.Phrase = new Phrase(total.ToString("N2"), font);
                t.AddCell(cell);

                cell = new PdfPCell(t.DefaultCell);
                cell.Phrase = new Phrase("");
                t.AddCell(cell);

                ct.AddElement(t);

                //------NonTax

                if (nontaxitems.Count > 0)
                {
                    t = new PdfPTable(new[] {15f, 25f, 15f, 15f, 30f});
                    t.WidthPercentage = 100;
                    t.DefaultCell.Border = Rectangle.NO_BORDER;
                    t.HeaderRows = 2;

                    cell = new PdfPCell(t.DefaultCell);
                    cell.Colspan = 5;
                    cell.Phrase = new Phrase("\n\nNon Tax-Deductible Items\n", boldfont);
                    t.AddCell(cell);

                    t.DefaultCell.Border = Rectangle.BOTTOM_BORDER;
                    t.AddCell(new Phrase("Date", boldfont));
                    t.AddCell(new Phrase("Description", boldfont));
                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Phrase = new Phrase("Amount", boldfont);
                    t.AddCell(cell);
                    t.AddCell(new Phrase("", boldfont));
                    t.AddCell(new Phrase("", boldfont));

                    t.DefaultCell.Border = Rectangle.NO_BORDER;

                    var ntotal = 0m;
                    foreach (var c in nontaxitems)
                    {
                        t.AddCell(new Phrase(c.ContributionDate.ToShortDateString(), font));
                        t.AddCell(new Phrase(c.Fund, font));
                        cell = new PdfPCell(t.DefaultCell);
                        cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell.Phrase = new Phrase(c.ContributionAmount.ToString("N2"), font);
                        t.AddCell(cell);
                        t.AddCell(new Phrase("", boldfont));
                        if (ShowNotes)
                            t.AddCell(new Phrase(c.Description, font));
                        else
                            t.AddCell(new Phrase("", font));

                        ntotal += (c.ContributionAmount);
                    }
                    t.DefaultCell.Border = Rectangle.TOP_BORDER;
                    cell = new PdfPCell(t.DefaultCell);
                    cell.Colspan = 2;
                    cell.Phrase = new Phrase("Total Non Tax-Deductible Items for period", boldfont);
                    t.AddCell(cell);
                    cell = new PdfPCell(t.DefaultCell);
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.Phrase = new Phrase(ntotal.ToString("N2"), font);
                    t.AddCell(cell);
                    t.AddCell(new Phrase("", boldfont));
                    t.AddCell(new Phrase("", boldfont));


                    ct.AddElement(t);
                }

                var status = 0;
                while (ColumnText.HasMoreText(status))
                {
                    ct.SetSimpleColumn(doc.Left, doc.Bottom, doc.Left + colwidth, pos);

                    status = ct.Go();
                    pos = doc.Top;
                    doc.NewPage();
                }

                runningtotals.Processed += 1;
                runningtotals.CurrSet = set;
                Db.SubmitChanges();
            }

            if (count == 0)
            {
                doc.NewPage();
                doc.Add(new Phrase("no data"));
            }
            doc.Close();

            if (set == LastSet())
                runningtotals.Completed = DateTime.Now;
            Db.SubmitChanges();
        }

        private class PageEvent : PdfPageEventHelper
        {
            private PdfContentByte dc;
            private BaseFont font;
            private NPages npages;
            private int pg;
            private PdfWriter writer;
            private Document document;

            public int set { get; set; }
            public int PeopleId { get; set; }
            public int NextPeopleId { get; set; }
            public Dictionary<int, int> FamilySet { get; set; }

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                this.writer = writer;
                this.document = document;
                base.OnOpenDocument(writer, document);
                font = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                dc = writer.DirectContent;
                if (set == 0)
                    FamilySet = new Dictionary<int, int>();
                npages = new NPages(dc);
            }

            public void EndPageSet()
            {
                if (npages == null)
                    return;
                npages.template.BeginText();
                npages.template.SetFontAndSize(font, 8);
                npages.template.ShowText(npages.n.ToString());
                if (set == 0)
                {
                    var list = FamilySet.Where(kp => kp.Value == 0).ToList();
                    foreach (var kp in list)
                        if (kp.Value == 0)
                            FamilySet[kp.Key] = npages.n;
                }
                pg = 1;
                npages.template.EndText();
                npages = new NPages(dc);
            }

            public void StartPageSet()
            {
                npages.juststartednewset = true;
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                base.OnEndPage(writer, document);
                if (npages.juststartednewset)
                    EndPageSet();

                string text;
                float len;

                text = $"id: {PeopleId}   Page {pg} of ";
                PeopleId = NextPeopleId;
                len = font.GetWidthPoint(text, 8);
                dc.BeginText();
                dc.SetFontAndSize(font, 8);
                dc.SetTextMatrix(30, 30);
                dc.ShowText(text);
                dc.EndText();
                dc.AddTemplate(npages.template, 30 + len, 30);
                npages.n = pg++;
            }

            public override void OnCloseDocument(PdfWriter writer, Document document)
            {
                base.OnCloseDocument(writer, document);
                EndPageSet();
            }

            private class NPages
            {
                public readonly PdfTemplate template;
                public bool juststartednewset;
                public int n;

                public NPages(PdfContentByte dc)
                {
                    template = dc.CreateTemplate(50, 50);
                }
            }
        }
    }
}
