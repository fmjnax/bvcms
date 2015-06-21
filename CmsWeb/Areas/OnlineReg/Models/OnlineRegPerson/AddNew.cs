﻿using System;
using System.Data.Linq;
using System.Linq;
using System.Web.Mvc;
using CmsData;
using CmsData.Codes;
using UtilityExtensions;

namespace CmsWeb.Areas.OnlineReg.Models
{
    public partial class OnlineRegPersonModel
    {
        public bool PrepareToAddNewPerson(ModelStateDictionary modelstate, int id)
        {
            modelState = modelstate;
            Index = id;

            Parent.HistoryAdd("ShowMoreInfo id=" + id);

            if (org != null && Found == true)
            {
                if (!Parent.SupportMissionTrip)
                    IsFilled = org.RegLimitCount(DbUtil.Db) >= org.Limit;
                if (IsFilled)
                    modelState.AddModelError(Parent.GetNameFor(mm => mm.List[id].DateOfBirth), "Sorry, but registration is closed.");
                if (Found == true)
                    FillPriorInfo();
                return true;
            }
            if (id > 0)
            {
                // copy address because probably in same family
                var pp = Parent.List[id - 1];
                AddressLineOne = pp.AddressLineOne;
                AddressLineTwo = pp.AddressLineTwo;
                City = pp.City;
                State = pp.State;
                ZipCode = pp.ZipCode;
                Country = pp.Country;
                Log("WillAddToSameFamily");
            }
            else
                Log("WillAddToNewFamily");
            ShowAddress = true;
            return false;
        }

        public string AddNew(ModelStateDictionary modelstate, int id)
        {
            modelState = modelstate;
            Index = id;

            if (modelState.IsValid)
            {
                if (Parent.ManagingSubscriptions())
                {
                    IsNew = true;
                    Parent.ConfirmManageSubscriptions();
                    DbUtil.Db.SubmitChanges();
                    return "ManageSubscriptions/OneTimeLink";
                }
                if (Parent.OnlinePledge())
                {
                    IsNew = true;
                    Parent.SendLinkForPledge();
                    DbUtil.Db.SubmitChanges();
                    return "ManagePledge/OneTimeLink";
                }
                if (Parent.ManageGiving())
                {
                    IsNew = true;
                    Parent.SendLinkToManageGiving();
                    DbUtil.Db.SubmitChanges();
                    return "ManageGiving/OneTimeLink";
                }
                if (ComputesOrganizationByAge())
                {
                    if (org == null)
                    {
                        Log("NoApproprateAgeGroup");
                        modelState.AddModelError(Parent.GetNameFor(mm => mm.List[id].Found), "Sorry, cannot find an appropriate age group");
                    }
                    else if (org.RegEnd.HasValue && DateTime.Now > org.RegEnd)
                    {
                        Log("Closed");
                        modelState.AddModelError(Parent.GetNameFor(mm => mm.List[id].Found), "Sorry, registration has ended for that group");
                    }
                    else if (org.OrganizationStatusId == OrgStatusCode.Inactive)
                    {
                        Log("Inactive");
                        modelState.AddModelError(Parent.GetNameFor(mm => mm.List[id].Found), "Sorry, that group is inactive");
                    }
                }
                else if (!ManageSubscriptions())
                {
                    if (!Parent.SupportMissionTrip)
                        IsFilled = org.RegLimitCount(DbUtil.Db) >= org.Limit;
                    if (IsFilled)
                    {
                        Log("Filled");
                        modelState.AddModelError(Parent.GetNameFor(mm => mm.List[id].Found), "Sorry, registration is filled");
                    }
                }
                IsNew = true;
            }
            IsValidForExisting = modelState.IsValid == false;
            if (IsNew)
                FillPriorInfo();
            if (org != null && FinishedFindingOrAddingRegistrant && ComputesOrganizationByAge())
                classid = org.OrganizationId;
            return null;
        }
        internal bool AnonymousReRegistrant()
        {
            if (Found != true || Parent.Orgid == null) 
                return false;
            if (!setting.AllowReRegister) 
                return false;
            var om = Parent.org.OrganizationMembers.SingleOrDefault(mm => mm.PeopleId == PeopleId);
            if (om == null) 
                return false;

            Parent.ConfirmReregister();
            DbUtil.Db.SubmitChanges();
            return true;
        }
        public void AddPerson(Person p, int entrypoint)
        {
            Family f;
            if (p == null)
                f = new Family
                {
                    AddressLineOne = AddressLineOne,
                    AddressLineTwo = AddressLineTwo,
                    CityName = City,
                    StateCode = State,
                    ZipCode = ZipCode,
                    CountryName = Country,
                    HomePhone = Phone,
                };
            else
                f = p.Family;
            DbUtil.Db.SubmitChanges();

            var position = DbUtil.Db.ComputePositionInFamily(age, married == 20 , f.FamilyId) ?? 10;
            _person = Person.Add(f, position,
                null, FirstName.Trim(), null, LastName.Trim(), DateOfBirth, married == 20, gender ?? 0,
                    OriginCode.Enrollment, entrypoint);
            person.EmailAddress = EmailAddress.Trim();
            person.SendEmailAddress1 = true;
            person.CampusId = DbUtil.Db.Setting("DefaultCampusId", "").ToInt2();
            person.CellPhone = Phone.GetDigits();

            if (count == 0)
                person.Comments = "Added during online registration because record was not found";
            else if(count > 1)
                person.Comments = "Added during online registration because there was more than 1 match";

            DbUtil.Db.SubmitChanges();
            DbUtil.Db.Refresh(RefreshMode.OverwriteCurrentValues, person);
            PeopleId = person.PeopleId;
            Log("AddPerson");
        }
    }
}