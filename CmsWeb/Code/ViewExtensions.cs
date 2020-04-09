using CmsData;
using CmsWeb.Areas.OnlineReg.Models;
using CmsWeb.Code;
using CmsWeb.Membership;
using Elmah;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.Web.Script.Serialization;
using UtilityExtensions;

namespace CmsWeb
{
    public enum ListType
    {
        Ordered,
        Unordered,
        TableCell
    }

    public static class ModelStateDictionaryExtensions
    {
        public static void SetModelValue(this ModelStateDictionary modelState, string key, object rawValue)
        {
            modelState.SetModelValue(key, new ValueProviderResult(rawValue, string.Empty, CultureInfo.InvariantCulture));
        }
    }

    public static class ViewExtensions2
    {
        public static CMSDataContext CurrentDatabase => DbUtil.Db;

        public static User CurrentUser => CurrentDatabase.CurrentUser;

        public static string jqueryGlobalizeCulture
        {
            get
            {
                //Determine culture - GUI culture for preference, user selected culture as fallback
                var currentCulture = CultureInfo.CurrentCulture;
                var filePattern = "/Content/touchpoint/lib/jquery-globalize/js/cultures/globalize.culture.{0}.js";
                var regionalisedFileToUse = string.Format(filePattern, "en-US"); //Default localisation to use

                //Try to pick a more appropriate regionalisation
                if (File.Exists(HostingEnvironment.MapPath(string.Format(filePattern, currentCulture.Name)))) //First try for a globalize.culture.en-GB.js style file
                {
                    regionalisedFileToUse = string.Format(filePattern, currentCulture.Name);
                }
                else if (File.Exists(HostingEnvironment.MapPath(string.Format(filePattern, currentCulture.TwoLetterISOLanguageName)))) //That failed; now try for a globalize.culture.en.js style file
                {
                    regionalisedFileToUse = string.Format(filePattern, currentCulture.TwoLetterISOLanguageName);
                }

                return regionalisedFileToUse;
            }
        }

        public static string CmsHost => CurrentDatabase.CmsHost;

        public static string GridClass => "table table-condensed table-striped notwide grid2 centered";

        public static MvcHtmlString DivValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression)
        {
            try
            {
                var msg = helper.ValidationMessageFor(expression);
                if (msg == null)
                {
                    return null;
                }

                var s = msg.ToString();
                if (s.HasValue() && s.Contains("field-validation-valid"))
                {
                    return null;
                }

                if (!s.HasValue())
                {
                    return null;
                }

                var div = new TagBuilder("div");
                div.AddCssClass("alert alert-danger");
                div.InnerHtml = s;
                return new MvcHtmlString(div.ToString());
            }
            catch (Exception ex)
            {
                var errorLog = ErrorLog.GetDefault(null);
                errorLog.Log(new Error(ex));
                return null;
            }
        }

        public static MvcHtmlString DivValidationMessage(this HtmlHelper helper, string field, string @class = null)
        {
            try
            {
                var msg = helper.ValidationMessage(field);
                if (msg == null)
                {
                    return null;
                }

                var s = msg.ToString();
                if (s.HasValue() && s.Contains("field-validation-valid"))
                {
                    return null;
                }

                if (!s.HasValue())
                {
                    return null;
                }

                var div = new TagBuilder("div");
                div.AddCssClass("alert alert-danger");
                if (@class.HasValue())
                {
                    div.AddCssClass(@class);
                }

                div.InnerHtml = s;
                return new MvcHtmlString(div.ToString());
            }
            catch (Exception ex)
            {
                var errorLog = ErrorLog.GetDefault(null);
                errorLog.Log(new Error(ex));
                return null;
            }
        }

        public static HtmlString DivAlertBox(this HtmlHelper helper, string msg, string alerttype = "alert-danger")
        {
            if (!msg.HasValue())
            {
                return null;
            }

            var div = new TagBuilder("div");
            div.AddCssClass("alert");
            div.AddCssClass(alerttype);
            div.InnerHtml = msg;
            return new HtmlString(div.ToString());
        }

        public static HtmlString PersonPortrait(this HtmlHelper helper, int PeopleId, int ImgX, int ImgY, string CssClass="img-circle") {
            Person person = CurrentDatabase.People.Single(p => p.PeopleId == PeopleId);
            if (person.IsNull())
            {
                return null;
            }
            var div = new TagBuilder("div");
            var gender = person.Gender.Code;
            Picture picture = person.Picture ?? new Picture();  // if there's no picture for this person, use the default
            var portraitUrl = gender == "M" ? picture.MediumMaleUrl : gender == "F" ? picture.MediumFemaleUrl : picture.MediumUrl;
            var portraitBgPos = picture.X.HasValue || picture.Y.HasValue ? $"{picture.X.GetValueOrDefault()}% {picture.Y.GetValueOrDefault()}%" : "top";

            div.AddCssClass(CssClass);
            div.MergeAttribute("style", $"" +
                $"background-image: url({portraitUrl});" +
                $"height: {ImgY}px;" +
                $"width: {ImgX}px;" +
                $"display: inline-block;" +
                $"background-repeat: no-repeat;" +
                $"background-size: cover;" +
                $"background-position: {portraitBgPos};");
            return new HtmlString(div.ToString());
        }

        public static MvcHtmlString PartialFor<TModel, TProperty>(this HtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> expression, string partialViewName)
        {
            var name = ExpressionHelper.GetExpressionText(expression);
            var model = ModelMetadata.FromLambdaExpression(expression, helper.ViewData).Model;
            var viewData = new ViewDataDictionary(helper.ViewData) { TemplateInfo = new TemplateInfo { HtmlFieldPrefix = name } };
            return helper.Partial(partialViewName, model, viewData);
        }

        public static string GetNameFor<M, P>(this M model, Expression<Func<M, P>> ex)
        {
            return ExpressionHelper.GetExpressionText(ex);
        }

        public static string GetIdFor<M, P>(this M model, Expression<Func<M, P>> ex)
        {
            return ExpressionHelper.GetExpressionText(ex).ToSuitableId();
        }

        public static string ToFormattedList(this IEnumerable list, ListType listType)
        {
            var outerListFormat = "";
            var listFormat = "";

            switch (listType)
            {
                case ListType.Ordered:
                    outerListFormat = "<ol>{0}</ol>";
                    listFormat = "<li>{0}</li>";
                    break;
                case ListType.Unordered:
                    outerListFormat = "<ul>{0}</ul>";
                    listFormat = "<li>{0}</li>";
                    break;
                case ListType.TableCell:
                    outerListFormat = "{0}";
                    listFormat = "<td>{0}</td>";
                    break;
            }
            return string.Format(outerListFormat, ToFormattedList(list, listFormat));
        }

        public static string ToFormattedList(IEnumerable list, string format)
        {
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendFormat(format, item);
            }

            return sb.ToString();
        }

        public static string ToQueryString(this NameValueCollection form)
        {
            return string.Join("&", form.AllKeys.Select(a => HttpUtility.UrlEncode(a) + "=" + HttpUtility.UrlEncode(form[a])));
        }

        public static string GetSiteUrl(this ViewPage pg)
        {
            var Port = pg.ViewContext.HttpContext.Request.ServerVariables["SERVER_PORT"];
            if (Port == null || Port == "80" || Port == "443")
            {
                Port = "";
            }
            else
            {
                Port = ":" + Port;
            }

            var Protocol = pg.ViewContext.HttpContext.Request.ServerVariables["SERVER_PORT_SECURE"];
            if (Protocol == null || Protocol == "0")
            {
                Protocol = "http://";
            }
            else
            {
                Protocol = "http://";
            }

            var appPath = pg.ViewContext.HttpContext.Request.ApplicationPath;
            if (appPath == "/")
            {
                appPath = "";
            }

            var sOut = Protocol + pg.ViewContext.HttpContext.Request.ServerVariables["SERVER_NAME"] + Port + appPath;
            return sOut;
        }

        public static HtmlString PageSizesDropDown(this HtmlHelper helper, string id, string onchange)
        {
            var tb = new TagBuilder("select");
            tb.MergeAttribute("id", id);
            if (onchange.HasValue())
            {
                tb.MergeAttribute("onchange", onchange);
            }

            var sb = new StringBuilder();
            foreach (var o in PageSizes(null))
            {
                var ot = new TagBuilder("option");
                ot.MergeAttribute("value", o.Value);
                if (o.Selected)
                {
                    ot.MergeAttribute("selected", "selected");
                }

                ot.SetInnerText(o.Text);
                sb.Append(ot);
            }
            tb.InnerHtml = sb.ToString();
            return new HtmlString(tb.ToString());
        }

        public static IEnumerable<SelectListItem> PageSizes(this HtmlHelper helper)
        {
            var sizes = new[] { 10, 25, 50, 75, 100, 200 };
            var list = new List<SelectListItem>();
            foreach (var size in sizes)
            {
                list.Add(new SelectListItem { Text = size.ToString() });
            }

            return list;
        }

        public static HtmlString SpanIf(this HtmlHelper helper, bool condition, string text, object htmlAttributes)
        {
            if (!condition)
            {
                return null;
            }

            var tb = new TagBuilder("span");
            var attr = new RouteValueDictionary(htmlAttributes);
            tb.InnerHtml = text;
            tb.MergeAttributes(attr);
            return new HtmlString(tb.ToString());
        }

        public static HtmlString Span(this HtmlHelper helper, string text, object htmlAttributes)
        {
            var tb = new TagBuilder("span");
            var attr = new RouteValueDictionary(htmlAttributes);
            tb.InnerHtml = text;
            tb.MergeAttributes(attr);
            return new HtmlString(tb.ToString());
        }

        private static string TryGetModel(this HtmlHelper helper, string name)
        {
            ModelState val;
            helper.ViewData.ModelState.TryGetValue(name, out val);
            string s = null;
            if (val != null)
            {
                s = val.Value.AttemptedValue;
            }

            return s;
        }

        public static HtmlString DropDownList2(this HtmlHelper helper, string name, IEnumerable<SelectListItem> list, bool visible)
        {
            var tb = new TagBuilder("select");
            tb.MergeAttribute("id", name);
            tb.MergeAttribute("name", name);
            if (!visible)
            {
                tb.MergeAttribute("style", "display: none");
            }

            var s = helper.TryGetModel(name);
            var sb = new StringBuilder();
            foreach (var o in list)
            {
                var ot = new TagBuilder("option");
                ot.MergeAttribute("value", o.Value);
                var selected = false;
                if (s.HasValue())
                {
                    selected = s == o.Value;
                }
                else if (o.Selected)
                {
                    selected = true;
                }

                if (selected)
                {
                    ot.MergeAttribute("selected", "selected");
                }

                ot.SetInnerText(o.Text);
                sb.Append(ot);
            }
            tb.InnerHtml = sb.ToString();
            return new HtmlString(tb.ToString());
        }

        public static HtmlString DropDownList3(this HtmlHelper helper, string id, string name, IEnumerable<SelectListItem> list, string value)
        {
            var tb = new TagBuilder("select");
            if (id.HasValue())
            {
                tb.MergeAttribute("id", id);
            }

            tb.MergeAttribute("name", name);
            var sb = new StringBuilder();
            foreach (var o in list)
            {
                var ot = new TagBuilder("option");
                ot.MergeAttribute("value", o.Value);
                if (value == o.Value)
                {
                    ot.MergeAttribute("selected", "selected");
                }

                ot.SetInnerText(o.Text);
                sb.Append(ot);
            }
            tb.InnerHtml = sb.ToString();
            return new HtmlString(tb.ToString());
        }

        public static HtmlString DropDownList4(this HtmlHelper helper, string id, string name, IEnumerable<SelectListItem> list, string value, string cssClass = "")
        {
            var tb = new TagBuilder("select");
            if (id.HasValue())
            {
                tb.MergeAttribute("id", id);
            }

            tb.MergeAttribute("name", name);
            if (cssClass.HasValue())
            {
                tb.MergeAttribute("class", cssClass);
            }

            var sb = new StringBuilder();
            foreach (var o in list)
            {
                var ot = new TagBuilder("option");
                ot.MergeAttribute("value", o.Value);
                if (value == o.Value)
                {
                    ot.MergeAttribute("selected", "selected");
                }

                ot.SetInnerText(o.Text);
                sb.Append(ot);
            }
            tb.InnerHtml = sb.ToString();
            return new HtmlString(tb.ToString());
        }

        public static HtmlString DropDownList4(this HtmlHelper helper, string id, string name, IEnumerable<OnlineRegPersonModel.SelectListItemFilled> list, string value, string cssClass = "")
        {
            var tb = new TagBuilder("select");
            if (id.HasValue())
            {
                tb.MergeAttribute("id", id);
            }

            tb.MergeAttribute("name", name);
            if (cssClass.HasValue())
            {
                tb.MergeAttribute("class", cssClass);
            }

            var sb = new StringBuilder();
            foreach (var o in list)
            {
                var ot = new TagBuilder("option");
                ot.MergeAttribute("value", o.Value);
                if (value == o.Value)
                {
                    ot.MergeAttribute("selected", "selected");
                }
                //				if (o.Filled)
                //					ot.MergeAttribute("disabled", "disabled");
                ot.SetInnerText(o.Text);
                sb.Append(ot);
            }
            tb.InnerHtml = sb.ToString();
            return new HtmlString(tb.ToString());
        }

        public static HtmlString TextBox2(this HtmlHelper helper, string name, bool visible)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "text");
            tb.MergeAttribute("id", name);
            tb.MergeAttribute("name", name);
            if (!visible)
            {
                tb.MergeAttribute("style", "display: none");
            }

            var s = helper.TryGetModel(name);
            var viewDataValue = Convert.ToString(helper.ViewData.Eval(name));
            tb.MergeAttribute("value", s ?? viewDataValue);
            return new HtmlString(tb.ToString());
        }

        public static HtmlString TextBox3(this HtmlHelper helper, string id, string name, string value)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "text");
            tb.MergeAttribute("id", id);
            tb.MergeAttribute("name", name);
            tb.MergeAttribute("value", value);
            return new HtmlString(tb.ToString());
        }

        public static HtmlString TextBox3(this HtmlHelper helper, string id, string name, string value, object htmlAttributes)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "text");
            tb.MergeAttribute("id", id);
            tb.MergeAttribute("name", name);
            tb.MergeAttribute("value", value);
            var attr = new RouteValueDictionary(htmlAttributes);
            tb.MergeAttributes(attr);
            ModelState state;
            if (helper.ViewData.ModelState.TryGetValue(name, out state) && (state.Errors.Count > 0))
            {
                tb.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }

            return new HtmlString(tb.ToString());
        }

        public static HtmlString TextBoxClass(this HtmlHelper helper, string name, string @class)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "text");
            tb.MergeAttribute("id", name);
            tb.MergeAttribute("name", name);
            tb.MergeAttribute("class", @class);
            var s = helper.TryGetModel(name);
            var viewDataValue = Convert.ToString(helper.ViewData.Eval(name));
            tb.MergeAttribute("value", s ?? viewDataValue);
            return new HtmlString(tb.ToString());
        }

        public static HtmlString HiddenFor2<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "hidden");
            var name = ExpressionHelper.GetExpressionText(expression);
            var v = htmlHelper.ViewData.Eval(name);
            var prefix = htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix;
            if (prefix.HasValue())
            {
                name = prefix + "." + name;
            }

            tb.MergeAttribute("name", name);
            if (v != null)
            {
                tb.MergeAttribute("value", v.ToString());
            }
            else
            {
                tb.MergeAttribute("value", "");
            }

            return new HtmlString(tb.ToString());
        }

        public static HtmlString HiddenFor3<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "hidden");
            var name = ExpressionHelper.GetExpressionText(expression);
            var v = htmlHelper.ViewData.Eval(name);
            var prefix = htmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix;
            if (prefix.HasValue())
            {
                name = prefix + "." + name;
            }

            tb.MergeAttribute("name", name);
            if (v != null)
            {
                tb.MergeAttribute("value", v.ToString());
            }
            else
            {
                tb.MergeAttribute("value", "");
            }

            return new HtmlString(tb.ToString());
        }

        public static HtmlString DatePicker(this HtmlHelper helper, string name)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "text");
            tb.MergeAttribute("id", name.Replace('.', '_'));
            tb.MergeAttribute("name", name);
            tb.MergeAttribute("class", "datepicker");
            var s = helper.TryGetModel(name);
            var viewDataValue = (DateTime?)helper.ViewData.Eval(name);
            tb.MergeAttribute("value", viewDataValue.FormatDate());
            return new HtmlString(tb.ToString());
        }

        public static HtmlString CheckBoxReadonly(this HtmlHelper helper, bool? ck)
        {
            var tb = new TagBuilder("input");
            tb.MergeAttribute("type", "checkbox");
            tb.MergeAttribute("disabled", "disabled");
            if (ck == true)
            {
                tb.MergeAttribute("checked", "checked");
            }

            return new HtmlString(tb.ToString());
        }

        public static HtmlString CodeDesc(this HtmlHelper helper, string name, IEnumerable<SelectListItem> list)
        {
            var tb = new TagBuilder("span");
            var viewDataValue = helper.ViewData.Eval(name);
            var i = (int?)viewDataValue ?? 0;

            var si = list.SingleOrDefault(v => v.Value == i.ToString());
            if (si != null)
            {
                tb.InnerHtml = si.Text;
            }
            else
            {
                tb.InnerHtml = "?";
            }

            return new HtmlString(tb.ToString());
        }

        public static HtmlString Hidden3(this HtmlHelper helper, string id, string name, object value)
        {
            var tb = new TagBuilder("input");
            if (id.HasValue())
            {
                tb.MergeAttribute("id", id);
            }

            tb.MergeAttribute("type", "hidden");
            tb.MergeAttribute("name", name);
            tb.MergeAttribute("value", value != null ? value.ToString() : "");
            return new HtmlString(tb.ToString());
        }
        public static HtmlString Hidden3(this HtmlHelper helper, string name, object value)
        {
            return helper.Hidden3(null, name, value);
        }

        public static HtmlString HiddenIf(this HtmlHelper helper, string name, bool? include)
        {
            if (include == true)
            {
                var tb = new TagBuilder("input");
                tb.MergeAttribute("type", "hidden");
                tb.MergeAttribute("id", name);
                tb.MergeAttribute("name", name);
                var viewDataValue = helper.ViewData.Eval(name);
                tb.MergeAttribute("value", viewDataValue.ToString());
                return new HtmlString(tb.ToString());
            }
            return new HtmlString("");
        }

        public static HtmlString IsRequired(this HtmlHelper helper, bool? Required)
        {
            //var tb = new TagBuilder("img");
            //tb.MergeAttribute("border", "0");
            //tb.MergeAttribute("width", "11");
            //tb.MergeAttribute("height", "12");
            //if ((Required ?? true) == true)
            //{
            //    tb.MergeAttribute("src", "/Content/images/req.gif");
            //    tb.MergeAttribute("alt", "req");
            //    return tb.ToString();
            //}
            //tb.MergeAttribute("src", "/Content/images/notreq.gif");
            //tb.MergeAttribute("alt", "not req");
            var tb = new TagBuilder("span");
            tb.MergeAttribute("class", "asterisk");
            if ((Required ?? true))
            {
                tb.InnerHtml = "*";
                return new HtmlString(tb.ToString());
            }
            tb.InnerHtml = "&nbsp;";
            return new HtmlString(tb.ToString());
        }

        public static HtmlString Required(this HtmlHelper helper)
        {
            return helper.IsRequired(true);
        }

        public static HtmlString NotRequired(this HtmlHelper helper)
        {
            return helper.IsRequired(false);
        }

        public static HtmlString HiddenIf(this HtmlHelper helper, string name, object value, bool? include)
        {
            if (include == true)
            {
                var tb = new TagBuilder("input");
                tb.MergeAttribute("type", "hidden");
                tb.MergeAttribute("id", name);
                tb.MergeAttribute("name", name);
                tb.MergeAttribute("value", value.ToString());
                return new HtmlString(tb.ToString());
            }
            return new HtmlString("");
        }

        public static HtmlString ValidationMessage2(this HtmlHelper helper, string name)
        {
            var m = helper.ViewData.ModelState[name];
            if (m == null || m.Errors.Count == 0)
            {
                return new HtmlString("");
            }

            var e = m.Errors[0].ErrorMessage;
            var b = new TagBuilder("span");
            b.AddCssClass(HtmlHelper.ValidationMessageCssClassName);
            b.SetInnerText(e);
            return new HtmlString(b.ToString());
        }

        public static string Json(this HtmlHelper html, string variableName, object model)
        {
            var tag = new TagBuilder("script");
            tag.Attributes.Add("type", "text/javascript");
            var jsonSerializer = new JavaScriptSerializer();
            tag.InnerHtml = "var " + variableName + " = " + jsonSerializer.Serialize(model) + ";";
            return tag.ToString();
        }

        public static string NameFor2<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            return htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(ExpressionHelper.GetExpressionText(expression));
        }

        public static string ErrorClass(this HtmlHelper helper, string field, string @class = "error")
        {
            var em = helper.ViewData.ModelState[field];
            return em?.Errors.Count > 0
                ? @class : "";
        }
        public static string ErrorMessage(this HtmlHelper helper, string field)
        {
            var em = helper.ViewData.ModelState[field];
            return em?.Errors.Count > 0
                ? em.Errors[0].ErrorMessage : "";
        }
        public static bool HasErrors(this HtmlHelper helper)
        {
            return helper.ViewData.ModelState.Any(vv => vv.Value.Errors.Count > 0);
        }

        public static MvcHtmlString ValidationMessageLabelFor<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, string errorClass = "error")
        {
            var elementId = html.IdFor(m => m).ToString();
            var normal = html.ValidationMessageFor(expression);
            if (normal != null)
            {
                var newValidator = Regex.Replace(normal.ToHtmlString(), @"<span([^>]*)>([^<]*)</span>", $@"<label for=""{elementId}"" $1>$2</label>", RegexOptions.IgnoreCase);
                if (!string.IsNullOrWhiteSpace(errorClass))
                {
                    newValidator = newValidator.Replace("field-validation-error", errorClass);
                }

                return MvcHtmlString.Create(newValidator);
            }
            return null;
        }

        public static string RenderPartialViewToString(Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }

        public static IHtmlString TextBoxFor2<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool useNativeUnobtrusiveAttributes,
            string format = null,
            object htmlAttributes = null)
        {
            // Return to native if true not passed
            if (!useNativeUnobtrusiveAttributes)
            {
                return htmlHelper.TextBoxFor(expression, format, htmlAttributes);
            }

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var attributes = Mapper.GetUnobtrusiveValidationAttributes(htmlHelper, expression, htmlAttributes, metadata);

            if (attributes.ContainsKey("data-rule-date") && attributes.ContainsKey("data-rule-dateandtimevalid"))
            {
                attributes.Remove("data-rule-date");
            }

            var textBox = Mapper.GenerateHtmlWithoutMvcUnobtrusiveAttributes(() =>
                htmlHelper.TextBoxFor(expression, format, attributes));

            return textBox;
        }

        public static IHtmlString CheckBoxFor2<TModel>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, bool>> expression,
            bool useNativeUnobtrusiveAttributes,
            object htmlAttributes = null)
        {
            // Return to native if true not passed
            if (!useNativeUnobtrusiveAttributes)
            {
                return htmlHelper.CheckBoxFor(expression, htmlAttributes);
            }

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var attributes = Mapper.GetUnobtrusiveValidationAttributes(htmlHelper, expression, htmlAttributes, metadata);
            var value = (bool)ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData).Model;

            var checkBox = Mapper.GenerateHtmlWithoutMvcUnobtrusiveAttributes(() =>
                htmlHelper.CheckBoxFor(expression, value, attributes));

            return checkBox;
        }

        public static IHtmlString RadioButtonFor2<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool useNativeUnobtrusiveAttributes,
            object value,
            object htmlAttributes = null)
        {
            // Return to native if true not passed
            if (!useNativeUnobtrusiveAttributes)
            {
                return htmlHelper.RadioButtonFor(expression, value, htmlAttributes);
            }

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var attributes = Mapper.GetUnobtrusiveValidationAttributes(htmlHelper, expression, htmlAttributes, metadata);

            var radioButton = Mapper.GenerateHtmlWithoutMvcUnobtrusiveAttributes(() =>
                htmlHelper.RadioButtonFor(expression, value, attributes));

            return radioButton;
        }

        public static IHtmlString DropDownListFor2<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool useNativeUnobtrusiveAttributes,
            IEnumerable<SelectListItem> selectList,
            string optionLabel = null,
            object htmlAttributes = null)
        {
            // Return to native if true not passed
            if (!useNativeUnobtrusiveAttributes)
            {
                return htmlHelper.DropDownListFor(expression, selectList, optionLabel, htmlAttributes);
            }

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var attributes = Mapper.GetUnobtrusiveValidationAttributes(htmlHelper, expression, htmlAttributes, metadata);

            var dropDown = Mapper.GenerateHtmlWithoutMvcUnobtrusiveAttributes(() =>
                htmlHelper.DropDownListFor(expression, selectList, optionLabel, attributes));

            return dropDown;
        }

        public static IHtmlString DropDownListForCodeInfo<TProperty>(this HtmlHelper<CodeInfo> htmlHelper,
            Expression<Func<CodeInfo, TProperty>> expression,
            bool useNativeUnobtrusiveAttributes,
            IEnumerable<SelectListItem> selectList,
            string optionLabel = null,
            object htmlAttributes = null)
        {
            if (!useNativeUnobtrusiveAttributes)
            {
                return htmlHelper.DropDownListFor(m => m.Value, selectList, optionLabel, htmlAttributes);
            }

            var metadata = ModelMetadata.FromLambdaExpression(m => m, htmlHelper.ViewData);
            var attributes = Mapper.GetUnobtrusiveValidationAttributes(htmlHelper, m => m, htmlAttributes, metadata);

            var dropDown = Mapper.GenerateHtmlWithoutMvcUnobtrusiveAttributes(() =>
                htmlHelper.DropDownListFor(m => m.Value, selectList, optionLabel, attributes));

            return dropDown;
        }

        public static IHtmlString DisplayForIf<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool show, string templateName, object viewdata = null, object htmlAttributes = null)
        {
            if (!show)
            {
                return null;
            }

            if (templateName == null)
            {
                return htmlHelper.DisplayFor(expression, viewdata);
            }

            return htmlHelper.DisplayFor(expression, templateName, viewdata);
        }

        public static IHtmlString DisplayForIf<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool show, object viewdata = null)
        {
            return htmlHelper.DisplayForIf(expression, show, null, viewdata);
        }

        public static IHtmlString EditorForIf<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool show, string templateName, object viewdata = null, object htmlAttributes = null)
        {
            if (!show)
            {
                return null;
            }

            if (templateName == null)
            {
                return htmlHelper.EditorFor(expression, viewdata);
            }

            return htmlHelper.EditorFor(expression, templateName, viewdata);
        }

        public static IHtmlString EditorForIf<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            bool show, object viewdata = null)
        {
            return htmlHelper.EditorForIf(expression, show, null, viewdata);
        }

        public static void SetExcelHeader(this ExcelWorksheet ws, params string[] headers)
        {
            var col = 0;
            foreach (var h in headers)
            {
                col++;
                var c = ws.Cells[1, col];
                c.Value = h;
            }
            var range = ws.Cells[1, 1, 1, col];
            range.Style.Font.Name = "Calibri";
            range.Style.Font.Size = 13;
            range.Style.Font.Bold = true;
            range.Style.Font.Color.SetColor(Color.FromArgb(68, 84, 106));
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
            range.Style.Border.Bottom.Color.SetColor(Color.FromArgb(172, 204, 234));
        }

        public static HtmlString GoogleFonts()
        {
            return new HtmlString(@"<link href=""https://fonts.googleapis.com/css?family=Open+Sans:300italic,400italic,400,600,300,700"" rel=""stylesheet"">");
        }

        public static HtmlString GoogleAnalytics()
        {
            var s = ConfigurationManager.AppSettings["GoogleAnalytics"];
            if (s.HasValue())
            {
                return new HtmlString($"<script>{s}</script>");
            }

            return null;
        }

        private static HtmlString IncludeOnce(string tag)
        {
            if (!HttpContextFactory.Current.Items.Contains(tag))
            {
                HttpContextFactory.Current.Items.Add(tag, true);
                return new HtmlString(tag);
            }
            return new HtmlString("");
        }

        public static HtmlString GoogleCharts()
        {
            return IncludeOnce(@"<script type=""text/javascript"" src=""https://www.gstatic.com/charts/loader.js""></script>");
        }

        public static HtmlString GoogleReCaptcha()
        {
            return IncludeOnce(@"<script src=""https://www.google.com/recaptcha/api.js""></script>");
        }

        public static HtmlString OldStyles()
        {
            return Fingerprint.Css("/content/styles/bundle.stylecss.css");
        }

        public static HtmlString NewStyles()
        {
            return Fingerprint.Css("/content/css/bundle.new2css.css");
        }

        public static HtmlString FixupsCss()
        {
            return Fingerprint.Css("/content/css/Fixups2.css");
        }

        public static string Bootstrap3Css()
        {
            return @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/3.4.1/css/bootstrap.min.css"">
<link rel=""stylesheet"" href=""/Content/css/OnlineReg2.css?v=2"">
<link rel=""stylesheet"" href=""/Content/css/fixups3.css"">
";
        }
        public static HtmlString Bootstrap3()
        {
            return IncludeOnce(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/3.4.1/js/bootstrap.min.js""></script>");
        }

        public static HtmlString BootstrapToggleCss()
        {
            return new HtmlString(@"<link rel=""stylesheet"" href=""https://gitcdn.github.io/bootstrap-toggle/2.2.2/css/bootstrap-toggle.min.css"">");
        }
        public static HtmlString BootstrapToggle()
        {
            return new HtmlString(@"<script src=""https://gitcdn.github.io/bootstrap-toggle/2.2.2/js/bootstrap-toggle.min.js""></script>");
        }

        public static HtmlString FontAwesome()
        {
            return new HtmlString(@"<link href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css"" rel=""stylesheet"">");
        }

        public static HtmlString CkEditor()
        {
            return new HtmlString(@"<script src=""https://cdn.ckeditor.com/4.5.11/full/ckeditor.js"" type=""text/javascript""></script>");
        }

        public static HtmlString UnlayerEditor()
        {
            return new HtmlString(@"<script src=""https://editor.unlayer.com/embed.js""></script>");
        }

        public static HtmlString jQueryMobile()
        {
            return IncludeOnce(@"<script src=""https://code.jquery.com/mobile/1.4.5/jquery.mobile-1.4.5.min.js""></script>");
        }

        public static HtmlString jQueryMobileCss()
        {
            return new HtmlString(@"<link rel=""stylesheet"" href=""https://code.jquery.com/mobile/1.4.5/jquery.mobile-1.4.5.min.css"" />");
        }

        public static HtmlString jQuery()
        {
            return IncludeOnce(@"<script src=""https://ajax.googleapis.com/ajax/libs/jquery/1.11.2/jquery.min.js""></script>");
        }

        public static HtmlString jQueryUICss()
        {
            return new HtmlString(@"<link rel=""stylesheet"" href=""https://ajax.googleapis.com/ajax/libs/jqueryui/1.11.3/themes/smoothness/jquery-ui.css"" />");
        }

        public static HtmlString jQueryUI()
        {
            return IncludeOnce(@"<script src=""https://ajax.googleapis.com/ajax/libs/jqueryui/1.11.3/jquery-ui.min.js""></script>");
        }

        public static HtmlString jQueryValidation()
        {
            return IncludeOnce(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/jquery-validate/1.19.0/jquery.validate.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/jquery-validate/1.19.0/additional-methods.min.js""></script>");
        }

        public static HtmlString Vue()
        {
            return IncludeOnce(@"<script src=""https://cdn.jsdelivr.net/npm/vue""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/vue-resource/1.5.1/vue-resource.min.js""></script>");
        }

        public static HtmlString Moment()
        {
            return IncludeOnce(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/moment.js/2.9.0/moment.min.js"" type=""text/javascript""></script>");
        }

        public static HtmlString Jsapi()
        {
            return IncludeOnce(@"<script src=""https://www.google.com/jsapi"" async></script>");
        }

        public static HtmlString Humanize()
        {
            return new HtmlString(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/humanize-plus/1.8.2/humanize.min.js"" type=""text/javascript""></script>");
        }

        public static HtmlString Velocity()
        {
            return new HtmlString(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/velocity/1.2.3/velocity.min.js"" type=""text/javascript""></script>");
        }

        public static HtmlString LoDash()
        {
            return IncludeOnce(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/lodash.js/2.4.1/lodash.min.js""></script>");
        }

        public static HtmlString Sortable()
        {
            return new HtmlString(@"<script src=""https://cdn.jsdelivr.net/npm/\@shopify/draggable@1.0.0-beta.8/lib/sortable.js""></script>");
        }

        public static HtmlString Markdown(this HtmlHelper helper, string text)
        {
            return Markdown(text);
        }

        public static HtmlString Markdown(string text)
        {
            var output = PythonModel.Markdown(text);
            return new HtmlString(output);
        }

        public static bool ShowOrgSettingsHelp(this HtmlHelper helper)
        {
            return CurrentDatabase.UserPreference("ShowOrgSettingsHelp", "true") == "true";
        }

        public static string TouchPointLayout()
        {
            return "~/Views/Shared/_Layout.cshtml";
        }

        public static string TouchPointLayoutWithoutHeaderFooter()
        {
            return "~/Views/Shared/_LayoutNoHeaderFooter.cshtml";
        }

        public static string DbSetting(string name, string def)
        {
            return CurrentDatabase.Setting(name, def);
        }

        public static IEnumerable<Person> PeopleFromPidString(string pids)
        {
            return from p in CurrentDatabase.PeopleFromPidString(pids)
                   select p;
        }

        public static List<string> AllRoles()
        {
            return User.AllRoles(CurrentDatabase).Select(rr => rr.RoleName).ToList();
        }

        public static string StatusFlagsAll(int peopleId)
        {
            return CurrentDatabase.StatusFlagsAll(peopleId);
        }

        public static Content GetContent(int tId)
        {
            var t = from e in CurrentDatabase.Contents
                    where e.Id == tId
                    select e;
            var c = t.FirstOrDefault();
            return c;
        }

        public static string DatabaseErrorUrl(DbUtil.CheckDatabaseResult ret)
        {
            switch (ret)
            {
                case DbUtil.CheckDatabaseResult.DatabaseDoesNotExist:
                    return $"/Errors/DatabaseNotFound.aspx?dbname={Util.Host}";
                case DbUtil.CheckDatabaseResult.ServerNotFound:
                    return $"/Errors/DatabaseServerNotFound.aspx?server={Util.DbServer}";
                case DbUtil.CheckDatabaseResult.DatabaseExists:
                    return null;
            }
            return null;
        }

        public static CollectionItemNamePrefixScope BeginCollectionItem<TModel>(this HtmlHelper<TModel> html, string collectionName)
        {
            var itemIndex = GetCollectionItemIndex(collectionName);
            var collectionItemName = $"{collectionName}[{itemIndex}]";

            var indexField = new TagBuilder("input");
            indexField.MergeAttributes(new Dictionary<string, string>
            {
                {"name", $"{collectionName}.Index"},
                {"value", itemIndex},
                {"type", "hidden"},
                {"autocomplete", "off"}
            });

            return new CollectionItemNamePrefixScope(
                html.ViewData.TemplateInfo,
                collectionItemName,
                indexField.ToString(TagRenderMode.SelfClosing));
        }

        private static string GetCollectionItemIndex(string collectionIndexFieldName)
        {
            var previousIndices = (Queue<string>)HttpContextFactory.Current.Items[collectionIndexFieldName];
            if (previousIndices == null)
            {
                HttpContextFactory.Current.Items[collectionIndexFieldName] = previousIndices = new Queue<string>();

                var previousIndicesValues = HttpContextFactory.Current.Request[collectionIndexFieldName];
                if (!string.IsNullOrWhiteSpace(previousIndicesValues))
                {
                    foreach (var index in previousIndicesValues.Split(','))
                    {
                        previousIndices.Enqueue(index);
                    }
                }
            }

            return previousIndices.Count > 0 ? previousIndices.Dequeue() : Guid.NewGuid().ToString();
        }

        public static MvcHtmlString ValidationSummaryBootstrap(this HtmlHelper helper, bool closeable)
        {
            # region Equivalent view markup

            // var errors = ViewData.ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage));
            //
            // if (errors.Count() > 0)
            // {
            //     <div class="alert alert-danger alert-block alert-dismissable">
            //         <button type="button" class="close" data-dismiss="alert" aria-hidden="true">&times;</button>
            //         <strong>Validation error!</strong> Please fix the errors listed below and try again.
            //         <ul>
            //             @foreach (var error in errors)
            //             {
            //                 <li class="text-error">@error</li>
            //             }
            //         </ul>
            //     </div>
            // }

            # endregion

            var errors = helper.ViewContext.ViewData.ModelState.SelectMany(state => state.Value.Errors.Select(error => error.ErrorMessage));

            var errorCount = errors.Count();

            var div = new TagBuilder("div");
            if (errorCount == 0)
            {
                div.AddCssClass("validation-summary-valid");
                div.MergeAttribute("data-valmsg-summary", "true");

                if (closeable)
                {
                    div.AddCssClass("alert-dismissable");
                }

                var ul = new TagBuilder("ul");
                var li = new TagBuilder("li");
                li.MergeAttribute("style", "display:none;");
                ul.InnerHtml += li.ToString();
                div.InnerHtml += ul.ToString();
                return new MvcHtmlString(div.ToString());
            }

            div.AddCssClass("validation-summary-errors");
            div.MergeAttribute("data-valmsg-summary", "true");
            div.AddCssClass("alert");
            div.AddCssClass("alert-danger");

            div.AddCssClass("alert-block");

            if (closeable)
            {
                div.AddCssClass("alert-dismissable");

                var button = new TagBuilder("button");
                button.AddCssClass("close");
                button.MergeAttribute("type", "button");
                button.MergeAttribute("data-dismiss", "alert");
                button.MergeAttribute("aria-hidden", "true");
                button.InnerHtml = "&times;";
                div.InnerHtml += button.ToString();
            }

            div.InnerHtml += "<strong>Validation Error!</strong>&nbsp;&nbsp;Please fix the errors listed below and try again.";

            if (errorCount > 0)
            {
                var ul = new TagBuilder("ul");

                foreach (var error in errors)
                {
                    var li = new TagBuilder("li");
                    li.SetInnerText(error);
                    ul.InnerHtml += li.ToString();
                }

                div.InnerHtml += ul.ToString();
            }

            return new MvcHtmlString(div.ToString());
        }

        public static MvcHtmlString ValidationSummaryBootstrap(this HtmlHelper helper)
        {
            return ValidationSummaryBootstrap(helper, true);
        }

        public static string HttpsUrl(this HtmlHelper helper, string url)
        {
            var uri = new Uri(url);
            if (uri.Scheme.Equal("http"))
            {
                return "https" + url.Substring(4);
            }
            else if (!uri.Scheme.HasValue())
            {
                return "https://" + url;
            }
            return url;
        }

        public static HtmlString TwoFactorAuthSetupLink()
        {
            var value = "";
            if (MembershipService.IsTwoFactorAuthenticationEnabled(CurrentDatabase))
            {
                value = CurrentUser.MFAEnabled
                    ? @"<li><a href=""/AuthDisable"">Disable 2FA</a></li>"
                    : @"<li><a href=""/AuthSetup"">Enable 2FA</a></li>";
            }
            return new HtmlString(value);
        }

        public class HelpMessage
        {
            public string errorClass;
            public HtmlString message;
        }

        public class CollectionItemNamePrefixScope : IDisposable
        {
            private readonly string _previousPrefix;
            private readonly TemplateInfo _templateInfo;

            public CollectionItemNamePrefixScope(TemplateInfo templateInfo, string collectionItemName, string hiddenindex)
            {
                _templateInfo = templateInfo;

                _previousPrefix = templateInfo.HtmlFieldPrefix;
                templateInfo.HtmlFieldPrefix = collectionItemName;
                this.hiddenindex = hiddenindex;
            }

            public string hiddenindex { get; set; }

            public string SuitableId => _templateInfo.HtmlFieldPrefix.ToSuitableId();

            public string CollectionName => _templateInfo.HtmlFieldPrefix;

            public void Dispose()
            {
                _templateInfo.HtmlFieldPrefix = _previousPrefix;
            }
        }
    }
}
