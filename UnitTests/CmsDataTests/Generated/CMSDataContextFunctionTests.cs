﻿using CmsData;
using CmsData.Codes;
using SharedTestFixtures;
using Shouldly;
using System;
using System.Linq;
using UtilityExtensions;
using Xunit;

namespace CmsDataTests
{
    [Collection(Collections.Database)]
    public class CMSDataContextFunctionTests : FinanceTestBase
    {
        [Fact]
        public void GetTotalContributionsDonorTest()
        {
            var fromDate = new DateTime(2019, 1, 1);
            var toDate = new DateTime(2019, 7, 31);
            using (var db = CMSDataContext.Create(DatabaseFixture.Host))
            {
                var TotalAmmountContributions = db.Contributions
                    .Where(x => x.ContributionTypeId == ContributionTypeCode.CheckCash)
                    .Where(x => x.ContributionDate >= fromDate)
                    .Where(x => x.ContributionDate < toDate.AddDays(1))
                    .Sum(x => x.ContributionAmount) ?? 0;
                var TotalPledgeAmountContributions = db.Contributions
                    .Where(x => x.ContributionTypeId == ContributionTypeCode.Pledge)
                    .Where(x => x.ContributionDate >= fromDate)
                    .Where(x => x.ContributionDate < toDate.AddDays(1))
                    .Sum(x => x.ContributionAmount) ?? 0;

                var bundleHeader = MockContributions.CreateSaveBundle(db);
                var FirstContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 120, peopleId: 1);
                var SecondContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 500, peopleId: 1, contributionType: ContributionTypeCode.Pledge);

                var results = db.GetTotalContributionsDonor(fromDate, toDate, null, null, null, true, null, null, true).ToList();
                var actualContributionsAmount = results.Where(x => x.ContributionTypeId == ContributionTypeCode.CheckCash).Sum(x => x.Amount);
                var actualPledgesAmount = results.Where(x => x.ContributionTypeId == ContributionTypeCode.Pledge).Sum(x => x.PledgeAmount);

                actualContributionsAmount.ShouldBe(TotalAmmountContributions + 120);
                actualPledgesAmount.ShouldBe(TotalPledgeAmountContributions + 500);

                MockContributions.DeleteAllFromBundle(db, bundleHeader);
            }
        }

        [Fact]
        public void PledgesSummaryTest()
        {
            var fromDate = new DateTime(2019, 1, 1);
            using (var db = CMSDataContext.Create(DatabaseFixture.Host))
            {
                var bundleHeader = MockContributions.CreateSaveBundle(db);
                var FirstContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 100, peopleId: 1);
                var SecondContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 20, peopleId: 1);
                var Pledges = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 500, peopleId: 1, contributionType: ContributionTypeCode.Pledge);

                //Get amount contributed to the pledge
                var TotalAmmountContributions = db.Contributions
                    .Where(x => x.FundId == 1)
                    .Where(x => x.PeopleId == 1)
                    .Where(x => x.ContributionTypeId != ContributionTypeCode.Pledge)
                    .Sum(x => x.ContributionAmount) ?? 0;

                //Get Pledge amount
                var TotalPledgeAmount = db.Contributions
                    .Where(x => x.ContributionTypeId == ContributionTypeCode.Pledge && x.PeopleId == 1 && x.FundId == 1)
                    .Sum(x => x.ContributionAmount) ?? 0;

                var results = db.PledgesSummary(1);
                var actual = results.ToList().First();

                actual.AmountContributed.ShouldBe(TotalAmmountContributions);
                actual.AmountPledged.ShouldBe(TotalPledgeAmount);
                actual.Balance.ShouldBe((TotalPledgeAmount) - (TotalAmmountContributions) < 0 ? 0 : (TotalPledgeAmount) - (TotalAmmountContributions));

                MockContributions.DeleteAllFromBundle(db, bundleHeader);      
            }
        }

        [Fact]
        public void IsTopGiverTest()
        {
            var fromDate = new DateTime(2017, 4, 4);
            var toDate = new DateTime(2017, 7, 31);
            using (var db = CMSDataContext.Create(Util.Host))
            {
                // Cleaning Contribution garbage from previous tests
                db.ExecuteCommand("DELETE FROM dbo.BundleDetail; DELETE FROM dbo.BundleHeader; DELETE FROM dbo.ContributionTag; DELETE FROM dbo.Contribution;");

                var family = new Family();
                db.Families.InsertOnSubmit(family);
                db.SubmitChanges();

                var person = new Person
                {
                    Family = family,
                    FirstName = "MockPersonFirstName",
                    LastName = "MockPersonLastName",
                    EmailAddress = "MockPerson@example.com",
                    MemberStatusId = MemberStatusCode.Member,
                    PositionInFamilyId = PositionInFamily.PrimaryAdult,
                };

                db.People.InsertOnSubmit(person);
                db.SubmitChanges();

                var bundleHeader = MockContributions.CreateSaveBundle(db);
                var FirstContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 100, peopleId: person.PeopleId);
                var SecondContribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 20, peopleId: person.PeopleId);

                var FundIds = $"{FirstContribution.FundId},{SecondContribution.FundId}";
                var TopGiversResult = db.TopGivers(10, fromDate, toDate, FundIds).ToList();

                if(TopGiversResult.Count > 0)
                {
                    var TotalAmmountTopGivers = TopGiversResult[0].Amount;
                    TotalAmmountTopGivers.ShouldBe(120);
                }
                else
                {
                    TopGiversResult.ShouldNotBeNull();
                }

                MockContributions.DeleteAllFromBundle(db, bundleHeader);
            }
        }

        [Theory]
        [InlineData(1)] //Generic Envelopes  
        [InlineData(4)] //Online
        public void GetTotalContributionsDonor2Test(int BundleType)
        {
            var fromDate = new DateTime(2016, 4, 4);
            var toDate = new DateTime(2016, 7, 31);

            using (var db = CMSDataContext.Create(Util.Host))
            {
                var bundleHeader = MockContributions.CreateSaveBundle(db);
                bundleHeader.BundleHeaderTypeId = BundleType;
                db.SubmitChanges();

                var Contribution = MockContributions.CreateSaveContribution(db, bundleHeader, fromDate, 120, peopleId: 1);

                var TotalContributionsDonor = db.GetTotalContributionsDonor2(fromDate, toDate, null, null, true, null, null).ToList();

                var ammount = TotalContributionsDonor[0].Amount;
                ammount.ShouldBe(120);

                MockContributions.DeleteAllFromBundle(db, bundleHeader);
            }
        }
    }
}
