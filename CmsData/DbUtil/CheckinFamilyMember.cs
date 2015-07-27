using System;
using UtilityExtensions;

namespace CmsData.View
{
    public partial class CheckinFamilyMember
    {
        public string BirthDay => Util.FormatBirthday(BYear, BMon, BDay);

        public string DisplayName
        {
            get
            {
                if (Age <= 18)
                    return $"{Name} ({Age})";
                return Name;
            }
        }
        public string DisplayClass
        {
            get
            {
                string s = "";
                if (Location.HasValue())
                    if (!ClassX.StartsWith(Location))
                        s = Location + ", ";
                s += ClassX;
                if (Leader.HasValue())
                    s += ", " + Leader;
                return s;
            }
        }
        public string OrgName
        {
            get
            {
                string s = ClassX;
                if (Leader.HasValue())
                    s += ", " + Leader;
                return s;
            }
        }

        public string dob
        {
            get
            {
                var dt = DateTime.MinValue;
                DateTime? bd = null;
                if (DateTime.TryParse(BirthDay, out dt))
                    bd = dt;
                return bd.FormatDate();
            }
        }
    }
}
