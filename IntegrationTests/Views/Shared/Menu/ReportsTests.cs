using IntegrationTests.Support;
using SharedTestFixtures;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace IntegrationTests.Views.Shared.Menu
{
    [Collection(Collections.Webapp)]
    public class ReportsTests : AccountTestBase
    {
        [Theory]
        [MemberData(nameof(Data_Should_Show_Hide_Statistics_Menu))]
        public void Should_Show_Hide_Statistics_Menu(string[] Roles, bool expected)
        {
            username = RandomString();
            password = RandomString();
            var user = CreateUser(username, password, roles: Roles);
            Login();

            Find(text: "Reports").Click();
            var element = Find(text: "Vital Stats");

            var result = element != null;
            result.ShouldBe(expected);
        }

        public static IEnumerable<object[]> Data_Should_Show_Hide_Statistics_Menu =>
            new List<object[]>
            {
                new object[] { NoFinanceRoles, false},
                new object[] { FinanceRoles, true},
            };

        public static readonly string[] NoFinanceRoles =
        {
        "Access", "Edit", "Admin", "ApplicationReview",
        "Attendance", "BackgroundCheck", "Checkin",
        "ContentEdit", "Coupon", "Coupon2", "Delete",
        "Design", "Developer", "FundManager", "ManageEmails",
        "ManageEvents", "ManageGroups", "ManageOrgMembers",
        "ManageTransactions", "Manager", "Manager2",
        "Membership", "MissionGiving", "OrgLeadersOnly", "OrgTagger",
        "ScheduleEmails", "SendSMS", "Support", "ViewVolunteerApplication"
        };

        public static readonly string[] FinanceRoles = { "Access", "Finance", "FinanceAdmin" };
    }
}
