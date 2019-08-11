using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine.Connect.Entities;
using Sitecore.Commerce.Engine.Connect.Pipelines;
using Sitecore.Commerce.Engine.Connect.Pipelines.Arguments;

namespace Brother.Commerce.Engine.Connect.Pipelines.Customers
{
    public class TranslateEntityToParty : TranslateEntityToODataModel<TranslateEntityToPartyRequest, TranslateEntityToPartyResult, CommerceParty, Party>
    {
        protected override void Translate(
          CommerceParty source,
          Party destination,
          TranslateEntityToPartyResult result)
        {
            base.Translate(source, destination, result);
            destination.ExternalId = source.PartyId;
            destination.AddressName = source.Name;
            destination.FirstName = source.FirstName;
            destination.LastName = source.LastName;
            destination.Email = source.Email;
            destination.Address1 = source.Address1;
            destination.Address2 = source.Address2;
            destination.City = source.City;
            destination.State = source.RegionName ?? source.State;
            destination.StateCode = source.RegionCode;
            destination.Country = source.Country;
            destination.CountryCode = source.CountryCode;
            destination.ZipPostalCode = source.ZipPostalCode;
            destination.PhoneNumber = source.PhoneNumber;
            destination.IsPrimary = source.IsPrimary;
            destination.Organization = source.Company; 
        }
    }
}
