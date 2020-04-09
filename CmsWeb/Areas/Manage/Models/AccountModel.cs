using CmsData;
using CmsData.API;
using CmsWeb.Areas.Manage.Models;
using CmsWeb.Membership;
using CmsWeb.Properties;
using Elmah;
using ImageData;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Security;
using UtilityExtensions;

namespace CmsWeb.Models
{
    public class AccountModel
    {
        public AccountModel() { }
        public string GetNewFileName(string path)
        {
            while (File.Exists(path))
            {
                var ext = Path.GetExtension(path);
                var fn = Path.GetFileNameWithoutExtension(path) + "a" + ext;
                var dir = Path.GetDirectoryName(path);
                path = Path.Combine(dir, fn);
            }
            return path;
        }

        public string CleanFileName(string fn)
        {
            return fn.Replace(' ', '_')
                .Replace('(', '-')
                .Replace(')', '-')
                .Replace(',', '_')
                .Replace("#", "")
                .Replace("!", "")
                .Replace("$", "")
                .Replace("%", "")
                .Replace("&", "_")
                .Replace("'", "")
                .Replace("+", "-")
                .Replace("=", "-");
        }

        public static string GetValidToken(CMSDataContext db, string otltoken)
        {
            if (!otltoken.HasValue())
            {
                return null;
            }

            var guid = otltoken.ToGuid();
            if (guid == null)
            {
                return null;
            }

            var ot = db.OneTimeLinks.SingleOrDefault(oo => oo.Id == guid.Value);
            if (ot == null)
            {
                return null;
            }

            if (ot.Used)
            {
                return null;
            }

            if (ot.Expires.HasValue && ot.Expires < DateTime.Now)
            {
                return null;
            }

            ot.Used = true;
            db.SubmitChanges();
            return ot.Querystring;
        }

        private const string STR_UserName2 = "UserName2";

        public static string UserName2
        {
            get { return HttpContextFactory.Current.Items[STR_UserName2] as string; }
            set { HttpContextFactory.Current.Items[STR_UserName2] = value; }
        }

        public static UserValidationResult AuthenticateMobile(CMSDataContext cmsdb, CMSImageDataContext cmsidb, string requiredRole = null, bool checkOrgLeadersOnly = false, bool requirePin = false)
        {
            var userStatus = GetUserViaCredentials() ?? GetUserViaSessionToken(cmsdb, requirePin);

            if (userStatus == null)
            {
                return UserValidationResult.Invalid(UserValidationStatus.ImproperHeaderStructure, "Could not authenticate user, Authorization or SessionToken headers likely missing.", null);
            }

            if (!userStatus.IsValid)
            {
                return userStatus;
            }

            var user = userStatus.User;

            var roleProvider = CMSRoleProvider.provider;

            if (requiredRole != null)
            {
                if (!roleProvider.RoleExists("Checkin"))
                {
                    requiredRole = "Access";
                }

                if (!roleProvider.IsUserInRole(user.Username, requiredRole))
                {
                    userStatus.Status = UserValidationStatus.UserNotInRole;
                    return userStatus;
                }
            }

            UserName2 = user.Username;
            SetUserInfo(cmsdb, cmsidb, user.Username, deleteSpecialTags: false);
            //DbUtil.LogActivity("iphone auth " + user.Username);

            if (checkOrgLeadersOnly && !Util2.OrgLeadersOnlyChecked)
            {
                DbUtil.LogActivity("iphone leadersonly check " + user.Username);
                if (!Util2.OrgLeadersOnly && roleProvider.IsUserInRole(user.Username, "OrgLeadersOnly"))
                {
                    Util2.OrgLeadersOnly = true;
                    cmsdb.SetOrgLeadersOnly();
                    DbUtil.LogActivity("SetOrgLeadersOnly");
                }
                Util2.OrgLeadersOnlyChecked = true;
            }

            ApiSessionModel.SaveApiSession(cmsdb, userStatus.User, requirePin, HttpContextFactory.Current.Request.Headers["PIN"].ToInt2());

            return userStatus;
        }

        public static UserValidationResult AuthenticateMobile2(CMSDataContext cmsdb, CMSImageDataContext cmsidb, bool checkOrgLeadersOnly = false, bool requirePin = false)
        {
            var userStatus = GetUserViaCredentials() ?? GetUserViaSessionToken(cmsdb, requirePin);

            if (userStatus == null)
            {
                return UserValidationResult.Invalid(UserValidationStatus.ImproperHeaderStructure, "Could not authenticate user, Authorization or SessionToken headers likely missing.", null);
            }

            if (!userStatus.IsValid)
            {
                return userStatus;
            }

            var user = userStatus.User;

            var roleProvider = CMSRoleProvider.provider;

            UserName2 = user.Username;
            SetUserInfo(cmsdb, cmsidb, user.Username, deleteSpecialTags: false);

            if (checkOrgLeadersOnly && !Util2.OrgLeadersOnlyChecked)
            {
                if (!Util2.OrgLeadersOnly && roleProvider.IsUserInRole(user.Username, "OrgLeadersOnly"))
                {
                    Util2.OrgLeadersOnly = true;
                    cmsdb.SetOrgLeadersOnly();
                    DbUtil.LogActivity("SetOrgLeadersOnly");
                }
                Util2.OrgLeadersOnlyChecked = true;
            }

            CMSMembershipProvider.provider.SetAuthCookie(user.Username, false);
            ApiSessionModel.SaveApiSession(cmsdb, userStatus.User, requirePin, HttpContextFactory.Current.Request.Headers["PIN"].ToInt2());

            return userStatus;
        }

        public static UserValidationResult ResetSessionExpiration(CMSDataContext cmsdb, CMSImageDataContext cmsidb, string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return UserValidationResult.Invalid(UserValidationStatus.ImproperHeaderStructure, "Could not authenticate user, Authorization or SessionToken headers likely missing.", null);
            }

            var userStatus = AuthenticateMobile(cmsdb, cmsidb, requirePin: true);

            if (userStatus.Status == UserValidationStatus.Success
                || userStatus.Status == UserValidationStatus.PinExpired
                || userStatus.Status == UserValidationStatus.SessionTokenExpired)
            {
                var result = ApiSessionModel.ResetSessionExpiration(cmsdb, userStatus.User, HttpContextFactory.Current.Request.Headers["PIN"].ToInt2());
                if (!result)
                {
                    return UserValidationResult.Invalid(UserValidationStatus.PinInvalid);
                }

                userStatus.Status = UserValidationStatus.Success;
            }

            return userStatus;
        }

        public static void ExpireSessionToken(CMSDataContext db, string sessionToken)
        {
            ApiSessionModel.ExpireSession(db, Guid.Parse(sessionToken));
        }

        private static UserValidationResult GetUserViaSessionToken(CMSDataContext db, bool requirePin)
        {
            var sessionToken = HttpContextFactory.Current.Request.Headers["SessionToken"];
            if (string.IsNullOrEmpty(sessionToken))
            {
                return null;
            }

            var result = ApiSessionModel.DetermineApiSessionStatus(db, Guid.Parse(sessionToken), requirePin, HttpContextFactory.Current.Request.Headers["PIN"].ToInt2());

            switch (result.Status)
            {
                case ApiSessionStatus.SessionTokenNotFound:
                    return UserValidationResult.Invalid(UserValidationStatus.SessionTokenNotFound);
                case ApiSessionStatus.SessionTokenExpired:
                    return UserValidationResult.Invalid(UserValidationStatus.SessionTokenExpired, user: result.User);
                case ApiSessionStatus.PinExpired:
                    return UserValidationResult.Invalid(UserValidationStatus.PinExpired, user: result.User);
                case ApiSessionStatus.PinInvalid:
                    return UserValidationResult.Invalid(UserValidationStatus.PinInvalid);
            }

            return ValidateUserBeforeLogin(db, result.User.Username, HttpContextFactory.Current.Request.Url.OriginalString, result.User, userExists: true);
        }

        private static UserValidationResult GetUserViaCredentials()
        {
            string username;
            string password;

            var auth = HttpContextFactory.Current.Request.Headers["Authorization"];
            if (auth.HasValue())
            {
                var cred = Encoding.ASCII.GetString(
                    Convert.FromBase64String(auth.Substring(6))).SplitStr(":", 2);
                username = cred[0];
                password = cred[1];
            }
            else
            {
                // NOTE: this is necessary only for the old iOS application
                username = HttpContextFactory.Current.Request.Headers["username"];
                password = HttpContextFactory.Current.Request.Headers["password"];
            }

            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            {
                var creds = new NetworkCredential(username, password);
                UserName2 = creds.UserName;
                return AuthenticateLogon(creds.UserName, creds.Password, HttpContextFactory.Current.Request.Url.OriginalString, CMSDataContext.Create(HttpContextFactory.Current));
            }

            return null;
        }

        public static UserValidationResult AuthenticateLogon(string userName, string password, string url, CMSDataContext db)
        {
            var userQuery = db.Users.Where(uu =>
                (uu.Username == userName ||
                uu.Person.EmailAddress == userName ||
                uu.Person.EmailAddress2 == userName) &&
                uu.Person.DeceasedDate == null
                );

            var impersonating = false;
            User user = null;
            var userExists = false;
            try
            {
                userExists = userQuery.Any();
            }
            catch(Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                return UserValidationResult.Invalid(UserValidationStatus.BadDatabase, "bad database");
            }

            var failedPasswordCount = 0;
            foreach (var u in userQuery.ToList())
            {
                if (u.TempPassword != null && password == u.TempPassword)
                {
                    u.TempPassword = null;
                    if (password == "bvcms") // set this up so Admin/bvcms works until password is changed
                    {
                        u.Password = "";
                        u.MustChangePassword = true;
                    }
                    else
                    {
                        var mu = CMSMembershipProvider.provider.GetUser(userName, false);
                        mu?.UnlockUser();
                        CMSMembershipProvider.provider.AdminOverride = true;
                        mu?.ChangePassword(mu.ResetPassword(), password);
                        CMSMembershipProvider.provider.AdminOverride = false;
                        u.MustChangePassword = true;
                    }
                    u.IsLockedOut = false;
                    db.SubmitChanges();
                    user = u;
                    break;
                }

                if (password == db.Setting("ImpersonatePassword", Guid.NewGuid().ToString()))
                {
                    user = u;
                    impersonating = true;
                    Util.IsNonFinanceImpersonator = true;
                    break;
                }

                if (CMSMembershipProvider.provider.ValidateUser(u.Username, password))
                {
                    db.Refresh(RefreshMode.OverwriteCurrentValues, u);
                    user = u;
                    break;
                }

                failedPasswordCount = Math.Max(failedPasswordCount, u.FailedPasswordAttemptCount);
            }

            return ValidateUserBeforeLogin(db, userName, url, user, userExists, failedPasswordCount, impersonating);
        }

        private static UserValidationResult ValidateUserBeforeLogin(CMSDataContext db, string userName, string url, User user, bool userExists, int failedPasswordCount = 0, bool impersonating = false)
        {
            var maxInvalidPasswordAttempts = CMSMembershipProvider.provider.MaxInvalidPasswordAttempts;
            const string DEFAULT_PROBLEM = "There is a problem with your username and password combination. If you are using your email address, it must match the one we have on record. Try again or use one of the links below.";

            if (user == null && userExists)
            {
                DbUtil.LogActivity($"failed password #{failedPasswordCount} by {userName}");

                if (failedPasswordCount == maxInvalidPasswordAttempts)
                {
                    return UserValidationResult.Invalid(UserValidationStatus.TooManyFailedPasswordAttempts,
                        "Your account has been locked out for too many failed attempts, use the forgot password link, or notify an Admin");
                }

                return UserValidationResult.Invalid(UserValidationStatus.IncorrectPassword, DEFAULT_PROBLEM);
            }

            if (user == null)
            {
                DbUtil.LogActivity("attempt to login by non-user " + userName);
                return UserValidationResult.Invalid(UserValidationStatus.NoUserFound, DEFAULT_PROBLEM);
            }

            if (user.IsLockedOut)
            {
                NotifyAdmins(db, $"{userName} locked out #{user.FailedPasswordAttemptCount} on {url}",
                    $"{userName} tried to login at {Util.Now} but is locked out");

                return UserValidationResult.Invalid(UserValidationStatus.LockedOut,
                    $"Your account has been locked out for {maxInvalidPasswordAttempts} failed attempts in a short window of time, please use the forgot password link or notify an Admin");
            }

            if (!user.IsApproved)
            {
                NotifyAdmins(db, $"unapproved user {userName} logging in on {url}",
                    $"{userName} tried to login at {Util.Now} but is not approved");

                return UserValidationResult.Invalid(UserValidationStatus.UserNotApproved, DEFAULT_PROBLEM);
            }

            if (impersonating)
            {
                if (user.Roles.Contains("Finance"))
                {
                    NotifyAdmins(db, $"cannot impersonate Finance user {userName} on {url}",
                        $"{userName} tried to login at {Util.Now}");

                    return UserValidationResult.Invalid(UserValidationStatus.CannotImpersonateFinanceUser, DEFAULT_PROBLEM);
                }
            }

            if (user.Roles.Contains("APIOnly"))
            {
                return UserValidationResult.Invalid(UserValidationStatus.NoUserFound,
                    "Api User is limited to API use only, no interactive login allowed.");
            }

            return UserValidationResult.Valid(user);
        }

        public static UserValidationResult AuthenticateLogon(string userName, string password, HttpRequestBase Request, CMSDataContext db, CMSImageDataContext idb)
        {
            var status = AuthenticateLogon(userName, password, Request.Url.OriginalString, db);
            if (status.IsValid)
            {
                return UserValidationResult.Valid(status.User);
            }
            return UserValidationResult.Invalid(UserValidationStatus.IncorrectPassword, status.ErrorMessage);
        }

        public static void FinishLogin(string userName, CMSDataContext db, CMSImageDataContext idb, bool logEntry = true)
        {
            SetUserInfo(db, idb, userName, logEntry);
            FormsAuthentication.SetAuthCookie(userName, false);
            if (logEntry)
            {
                DbUtil.LogActivity($"User {userName} logged in");
            }
        }

        public static object AutoLogin(string userName, HttpRequestBase Request, CMSDataContext db, CMSImageDataContext idb)
        {
#if DEBUG
            FinishLogin(userName, db, idb, false);
#endif
            return null;
        }

        private static void NotifyAdmins(CMSDataContext db, string subject, string message)
        {
            IEnumerable<Person> notify = null;
            if (Roles.GetAllRoles().Contains("NotifyLogin"))
            {
                notify = CMSRoleProvider.provider.GetRoleUsers("NotifyLogin").Select(u => u.Person).Distinct();
            }
            else
            {
                notify = CMSRoleProvider.provider.GetRoleUsers("Admin").Select(u => u.Person).Distinct();
            }

            db.EmailRedacted(DbUtil.AdminMail, notify, subject, message);
        }

        public static User SetUserInfo(CMSDataContext cmsdb, CMSImageDataContext cmsidb, string username, bool deleteSpecialTags = true)
        {
            var i = (from u in cmsdb.Users
                     where u.Username == username
                     select new { u, u.Person.PreferredName }).SingleOrDefault();
            if (i == null)
            {
                return null;
            }

            if (i.u != null)
            {
                Util.UserThumbPictureBgPosition = "top";
                if (i.u.Person?.Picture != null)
                {
                    var picture = i.u.Person.Picture;
                    Util.UserThumbPictureUrl = picture.GetThumbUrl(cmsidb);
                    Util.UserThumbPictureBgPosition = picture.X.HasValue || picture.Y.HasValue ?
                        $"{picture.X.GetValueOrDefault()}% {picture.Y.GetValueOrDefault()}%"
                        : "top";
                }

                Util.UserEmail = i.u.EmailAddress;
                Util2.CurrentPeopleId = i.u.PeopleId.Value;
                Util.UserPreferredName = i.PreferredName;
                Util.UserFullName = i.u.Name;
                Util.UserFirstName = i.u.Person.FirstName;
            }
            else
            { 
                return null;
            }

            Util.ActivePerson = i.u.Name;
            if (deleteSpecialTags)
            {
                cmsdb.DeleteSpecialTags(cmsdb.UserPeopleId);
            }
            return i.u;
        }

        public static string CheckAccessRole(CMSDataContext db, string name)
        {
            if (!Roles.IsUserInRole(name, "Access") && !Roles.IsUserInRole(name, "OrgMembersOnly"))
            {
                if (db.UserPeopleId > 0)
                {
                    return $"/Person2/{db.UserPeopleId}";
                }

                if (name.HasValue())
                {
                    DbUtil.LogActivity($"user {name} loggedin without a role ");
                }

                FormsAuthentication.SignOut();
                return "/Errors/AccessDenied.htm";
            }

            if (Roles.IsUserInRole(name, "NoRemoteAccess") && DbUtil.CheckRemoteAccessRole)
            {
                NotifyAdmins(db, "NoRemoteAccess", $"{name} tried to login from {Util.Host}");
                return "NoRemoteAccess.htm";
            }

            return null;
        }

        public static User AddUser(CMSDataContext db, int id)
        {
            var p = db.People.Single(pe => pe.PeopleId == id);
            CMSMembershipProvider.provider.AdminOverride = true;
            var user = MembershipService.CreateUser(db, id);
            CMSMembershipProvider.provider.AdminOverride = false;
            user.MustChangePassword = false;
            db.SubmitChanges();
            return user;
        }

        public static void SendNewUserEmail(CMSDataContext db, string username)
        {
            var user = db.Users.First(u => u.Username == username);
            var body = db.ContentHtml("NewUserWelcome", Resource1.AccountModel_NewUserWelcome);
            body = body.Replace("{first}", user.Person.PreferredName);
            body = body.Replace("{name}", user.Person.Name);
            body = body.Replace("{cmshost}", db.Setting("DefaultHost", db.Host));
            body = body.Replace("{username}", user.Username);
            user.ResetPasswordCode = Guid.NewGuid();
            user.ResetPasswordExpires = DateTime.Now.AddHours(db.Setting("ResetPasswordExpiresHours", "24").ToInt());
            var link = db.ServerLink("/Account/SetPassword/" + user.ResetPasswordCode);
            body = body.Replace("{link}", link);
            db.SubmitChanges();
            db.EmailRedacted(DbUtil.AdminMail, user.Person, "New user welcome", body);
        }

        public static void ForgotPassword(CMSDataContext db, string username)
        {
            // first find a user with the email address or username
            string msg = null;
            var path = new StringBuilder();

            username = username.Trim();
            var q = db.Users.Where(uu =>
                uu.Username == username ||
                uu.Person.EmailAddress == username ||
                uu.Person.EmailAddress2 == username
                );
            if (!q.Any())
            {
                path.Append("u0");
                // could not find a user to match
                // so we look for a person without an account, to match the email address

                var minage = db.Setting("MinimumUserAge", "16").ToInt();
                var q2 = from uu in db.People
                         where uu.EmailAddress == username || uu.EmailAddress2 == username
                         where uu.Age == null || uu.Age >= minage
                         select uu;
                if (q2.Any())
                {
                    path.Append("p+");
                    // we found person(s), not a user
                    // we will compose an email for each of them to create an account
                    foreach (var p in q2)
                    {
                        var ot = new OneTimeLink
                        {
                            Id = Guid.NewGuid(),
                            Querystring = p.PeopleId.ToString()
                        };
                        db.OneTimeLinks.InsertOnSubmit(ot);
                        db.SubmitChanges();
                        var url = db.ServerLink($"/Account/CreateAccount/{ot.Id.ToCode()}");
                        msg = db.ContentHtml("ForgotPasswordReset", Resource1.AccountModel_ForgotPasswordReset);
                        msg = msg.Replace("{name}", p.Name);
                        msg = msg.Replace("{first}", p.PreferredName);
                        msg = msg.Replace("{email}", username);
                        msg = msg.Replace("{resetlink}", url);
                        db.SendEmail(Util.FirstAddress(DbUtil.AdminMail),
                            "touchpointsoftware new password link", msg, Util.ToMailAddressList(p.EmailAddress ?? p.EmailAddress2));
                    }
                    DbUtil.LogActivity($"ForgotPassword ('{username}', {path})");
                    return;
                }
                path.Append("p0");
                if (!Util.ValidEmail(username))
                {
                    DbUtil.LogActivity($"ForgotPassword ('{username}', {path})");
                    return;
                }
                path.Append("n0");

                msg = db.ContentHtml("ForgotPasswordBadEmail", Resource1.AccountModel_ForgotPasswordBadEmail);
                msg = msg.Replace("{email}", username);
                db.SendEmail(Util.FirstAddress(DbUtil.AdminMail),
                    "Forgot password request for " + db.Setting("NameOfChurch", "bvcms"),
                    msg, Util.ToMailAddressList(username));
                DbUtil.LogActivity($"ForgotPassword ('{username}', {path})");
                return;
            }
            path.Append("u+");

            // we found users who match,
            // so now we send the users who match the username or email a set of links to all their usernames

            var sb = new StringBuilder();
            var addrlist = new List<MailAddress>();
            foreach (var user in q)
            {
                Util.AddGoodAddress(addrlist, user.EmailAddress);
                user.ResetPasswordCode = Guid.NewGuid();
                user.ResetPasswordExpires = DateTime.Now.AddHours(db.Setting("ResetPasswordExpiresHours", "24").ToInt());
                var link = db.ServerLink($"/Account/SetPassword/{user.ResetPasswordCode}");
                sb.Append($@"{user.Name}, <a href=""{link}"">{user.Username}</a><br>");
                db.SubmitChanges();
            }
            msg = db.ContentHtml("ForgotPasswordReset2", Resource1.AccountModel_ForgotPasswordReset2);
            msg = msg.Replace("{email}", username);
            msg = msg.Replace("{resetlink}", sb.ToString());
            db.SendEmail(Util.FirstAddress(DbUtil.AdminMail),
                "TouchPoint password reset link", msg, addrlist);
            DbUtil.LogActivity($"ForgotPassword ('{username}', {path})");
        }
    }
}
