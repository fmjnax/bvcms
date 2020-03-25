using CmsData;
using CmsData.Registration;
using CmsWeb.Areas.OnlineReg.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UtilityExtensions;

namespace CmsWeb.Areas.OnlineReg.Controllers
{
    public partial class OnlineRegController
    {
        private const string ManagedGivingShellSettingKey = "UX-ManagedGivingShell";

        private Dictionary<int, Settings> _settings;
        public Dictionary<int, Settings> settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = HttpContext.Items["RegSettings"] as Dictionary<int, Settings>;
                }

                return _settings;
            }
        }

        public void SetHeaders(OnlineRegModel m2)
        {
            RequestManager.SessionProvider.Add("gobackurl", m2.URL);
            ViewBag.Title = m2.Header;
            SetHeaders2(m2.Orgid ?? m2.masterorgid ?? 0);
        }
        private void SetHeaders2(int id)
        {
            var org = CurrentDatabase.LoadOrganizationById(id);
            var shell = GetAlternativeManagedGivingShell(org.OrganizationId);

            if (!shell.HasValue() && (settings == null || !settings.ContainsKey(id)) && org != null)
            {
                var setting = CurrentDatabase.CreateRegistrationSettings(id);
                shell = CurrentDatabase.ContentOfTypeHtml(setting.ShellBs)?.Body;
            }
            if (!shell.HasValue() && settings != null && settings.ContainsKey(id))
            {
                shell = CurrentDatabase.ContentOfTypeHtml(settings[id].ShellBs)?.Body;
            }

            if (!shell.HasValue())
            {
                shell = CurrentDatabase.ContentOfTypeHtml("ShellDefaultBs")?.Body;
                if (!shell.HasValue())
                {
                    shell = CurrentDatabase.ContentOfTypeHtml("DefaultShellBs")?.Body;
                }
            }


            if (shell != null && shell.HasValue())
            {
                shell = shell.Replace("{title}", ViewBag.Title);
                var re = new Regex(@"(.*<!--FORM START-->\s*).*(<!--FORM END-->.*)", RegexOptions.Singleline);
                var t = re.Match(shell).Groups[1].Value.Replace("<!--FORM CSS-->", ViewExtensions2.Bootstrap3Css());
                ViewBag.hasshell = true;
                ViewBag.top = t;
                var b = re.Match(shell).Groups[2].Value;
                ViewBag.bottom = b;
            }
            else
            {
                ViewBag.hasshell = false;
            }
        }
        private void SetHeaders(int orgId)
        {
            Settings setting = null;
            var org = CurrentDatabase.LoadOrganizationById(orgId);
            if (org != null)
            {
                SetHeaders2(orgId);
                return;
            }

            var shell = GetAlternativeManagedGivingShell(orgId);
            if (!shell.HasValue() && (settings == null || !settings.ContainsKey(orgId)))
            {
                setting = CurrentDatabase.CreateRegistrationSettings(orgId);
                shell = DbUtil.Content(CurrentDatabase, setting.Shell, null);
            }
            if (!shell.HasValue() && settings != null && settings.ContainsKey(orgId))
            {
                shell = DbUtil.Content(CurrentDatabase, settings[orgId].Shell, null);
            }
            if (!shell.HasValue())
            {
                shell = DbUtil.Content(CurrentDatabase, "ShellDiv-" + orgId, DbUtil.Content(CurrentDatabase, "ShellDefault", ""));
            }

            var s = shell;
            if (s.HasValue())
            {
                var re = new Regex(@"(.*<!--FORM START-->\s*).*(<!--FORM END-->.*)", RegexOptions.Singleline);
                var t = re.Match(s).Groups[1].Value.Replace("<!--FORM CSS-->",
                ViewExtensions2.jQueryUICss() +
                "\r\n<link href=\"/Content/styles/onlinereg.css?v=8\" rel=\"stylesheet\" type=\"text/css\" />\r\n");
                ViewBag.hasshell = true;
                var b = re.Match(s).Groups[2].Value;
                ViewBag.bottom = b;
            }
            else
            {
                ViewBag.hasshell = false;
                ViewBag.header = DbUtil.Content(CurrentDatabase, "OnlineRegHeader-" + orgId,
                    DbUtil.Content(CurrentDatabase, "OnlineRegHeader", ""));
                ViewBag.top = DbUtil.Content(CurrentDatabase, "OnlineRegTop-" + orgId,
                    DbUtil.Content(CurrentDatabase, "OnlineRegTop", ""));
                ViewBag.bottom = DbUtil.Content(CurrentDatabase, "OnlineRegBottom-" + orgId,
                    DbUtil.Content(CurrentDatabase, "OnlineRegBottom", ""));
            }
        }

        private string GetAlternativeManagedGivingShell(int orgId)
        {
            var shell = string.Empty;
            var managedGivingShellSettingKey = ManagedGivingShellSettingKey;
            var campus = RequestManager.SessionProvider.Get<string>($"Campus-{orgId}"); // campus is only set for managed giving flow.
            if (!string.IsNullOrWhiteSpace(campus))
            {
                managedGivingShellSettingKey = $"{managedGivingShellSettingKey}-{campus.ToUpper()}";
            }
            var alternateShellSetting = CurrentDatabase.Settings.SingleOrDefault(x => x.Id == managedGivingShellSettingKey);
            if (alternateShellSetting != null)
            {
                var alternateShell = CurrentDatabase.Contents.SingleOrDefault(x => x.Name == alternateShellSetting.SettingX);
                if (alternateShell != null)
                {
                    shell = alternateShell.Body;
                }
            }

            return shell;
        }
    }
}
