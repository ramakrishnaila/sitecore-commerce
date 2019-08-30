using Custom.Commerce.Feature.Catalog;
using Custom.Commerce.XA.Feature.Cart.Models.InputModels;
using Custom.Commerce.XA.Feature.Cart.Models.JsonResults;
using Sitecore.Commerce.Engine.Connect.Entities;
using Sitecore.Commerce.Entities.Carts;
using Sitecore.Commerce.Entities.Shipping;
using Sitecore.Commerce.Services;
using Sitecore.Commerce.Services.Carts;
using Sitecore.Commerce.Services.Orders;
using Sitecore.Commerce.Services.Shipping;
using Sitecore.Commerce.XA.Feature.Cart.ExtensionMethods;
using Sitecore.Commerce.XA.Feature.Cart.Models;
using Sitecore.Commerce.XA.Feature.Cart.Models.InputModels;
using Sitecore.Commerce.XA.Feature.Cart.Models.JsonResults;
using Sitecore.Commerce.XA.Feature.Cart.Repositories;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Providers;
using Sitecore.Commerce.XA.Foundation.Connect;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Commerce.XA.Foundation.Connect.Utils;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.XA.Foundation.SitecoreExtensions.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;

namespace Custom.Commerce.XA.Feature.Cart.Repositories
{
    public class DeliveryRepository : CustomBaseCheckoutRepository, Custom.Commerce.XA.Feature.Cart.Repositories.IDeliveryRepository
    {
       
        IModelProvider _modelProvider;
        private ISearchManager _searchManager;
        public DeliveryRepository(
          IModelProvider modelProvider,
          IStorefrontContext storefrontContext,
          ICartManager cartManager,
          IOrderManager orderManager,
          IAccountManager accountManager,
          IShippingManager shippingManager,
          ICheckoutStepProvider checkoutStepProvider,
          IContext context)
          : base(modelProvider, storefrontContext, cartManager, orderManager, accountManager, checkoutStepProvider)
        {
            Assert.ArgumentNotNull((object)shippingManager, nameof(shippingManager));
            this.ShippingManager = shippingManager;
            this.Context = context;
            _modelProvider = modelProvider;
            _searchManager = DependencyResolver.Current.GetService<ISearchManager>();
        }

        public IShippingManager ShippingManager { get; protected set; }

        public IContext Context { get; }

        public virtual DeliveryRenderingModel GetDeliveryRenderingModel(
          IRendering rendering)
        {
            DeliveryRenderingModel model = this.ModelProvider.GetModel<DeliveryRenderingModel>();
            this.Init((BaseCommerceRenderingModel)model);
            model.Initialize(rendering);
            return model;
        }

        public CustomDeliveryDataJsonResult GetDeliveryData(
          IVisitorContext visitorContext)
        {
            CustomDeliveryDataJsonResult model = this.ModelProvider.GetModel<CustomDeliveryDataJsonResult>();
            if (!this.Context.IsExperienceEditor)
            {
                try
                {
                    ManagerResponse<CartResult, Sitecore.Commerce.Entities.Carts.Cart> currentCart = this.CartManager.GetCurrentCart(visitorContext, this.StorefrontContext, true);
                    if (!currentCart.ServiceProviderResult.Success)
                    {
                        model.SetErrors((ServiceProviderResult)currentCart.ServiceProviderResult);
                        return model;
                    }
                    Sitecore.Commerce.Entities.Carts.Cart result = currentCart.Result;

                    if (result.Lines != null)
                    {
                        if (result.Lines.Any<CartLine>())
                        {
                            model.Initialize(result, visitorContext);
                            this.AddShippingOptionsToModel(model, result);
                            if (model.Success)
                            {
                                this.AddEmailShippingMethodToResult(model, result);
                                if (model.Success)
                                {
									//This is the method we need to add for adding the available states to the delivery Json result which will be consumed by knock out to display the states dictionary in the dropdown.
                                    this.AddAvailableRegions((CustomDeliveryDataJsonResult)model);
                                    this.AddAvailableCountries((CustomBaseCheckoutDataJsonResult)model);
                                    if (model.Success)
                                    {
                                        if (Context.User.IsAuthenticated)
                                        {
                                            this.AddCustomUserInfo((CustomBaseCheckoutDataJsonResult)model);
                                        }
                                        if (model.Success)
                                            this.CheckIfEditing(visitorContext, model);
                                    }
                                }
                            }
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex, (object)this);
                    model.SetErrors(nameof(GetDeliveryData), ex);
                    return model;
                }
            }
            return model;
        }

     
         protected virtual void CheckIfEditing(
          IVisitorContext visitorContext,
          CustomBaseCheckoutDataJsonResult result)
        {
            if ((result.Cart == null || result.Cart.Shipments == null ? 0 : (result.Cart.Shipments.Count > 0 ? 1 : 0)) == 0)
                return;
            foreach (ShippingInfoJsonResult shipment1 in result.Cart.Shipments)
            {
                ShippingInfoJsonResult shipment = shipment1;
                if (shipment.LineIDs != null && shipment.LineIDs.Count > 0)
                {
                    CartLineJsonResult cartLineJsonResult = result.Cart.Lines.Find((l => l.ExternalCartLineId.Equals(shipment.LineIDs[0], StringComparison.OrdinalIgnoreCase)));
                    if (cartLineJsonResult != null && cartLineJsonResult.ShippingOptions != null && cartLineJsonResult.ShippingOptions.Count<ShippingOptionJsonResult>() > 0)
                    {
                        ShippingOptionJsonResult optionJsonResult = cartLineJsonResult.ShippingOptions.ElementAt<ShippingOptionJsonResult>(0);
                        if (optionJsonResult != null)
                        {
                            shipment.EditModeShippingOptionType = optionJsonResult.ShippingOptionType;
                            if (optionJsonResult.ShippingOptionType == ShippingOptionType.ShipToAddress)
                            {
                                AddressJsonResult addressJsonResult = result.Cart.Parties.Find((Predicate<AddressJsonResult>)(p => p.ExternalId.Equals(shipment.PartyID, StringComparison.OrdinalIgnoreCase)));
                                if (addressJsonResult != null)
                                {
                                    GetShippingMethodsInputModel inputModel = new GetShippingMethodsInputModel()
                                    {
                                        ShippingPreferenceType = ShippingOptionType.ShipToAddress.Value.ToString((IFormatProvider)CultureInfo.InvariantCulture),
                                        ShippingAddress = new PartyInputModel()
                                    };
                                    inputModel.ShippingAddress.ExternalId = addressJsonResult.ExternalId;
                                    inputModel.ShippingAddress.Address1 = addressJsonResult.Address1;
                                    inputModel.ShippingAddress.City = addressJsonResult.City;
                                    inputModel.ShippingAddress.State = addressJsonResult.State;
                                    inputModel.ShippingAddress.ZipPostalCode = addressJsonResult.ZipPostalCode;
                                    inputModel.ShippingAddress.Country = addressJsonResult.Country;
                                    inputModel.ShippingAddress.IsPrimary = addressJsonResult.IsPrimary;
                                    inputModel.Lines = new List<CartLineInputModel>();
                                    ShippingMethodsJsonResult shippingMethods = this.GetShippingMethods(visitorContext, inputModel);
                                    if (shippingMethods != null)
                                    {
                                        shipment.ShipmentEditModel = this.ModelProvider.GetModel<ShipmentEditModeDataJsonResult>();
                                        shipment.ShipmentEditModel.ShippingMethods = shippingMethods.ShippingMethods;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void AddShippingOptionsToModel(CustomDeliveryDataJsonResult jsonResult, Sitecore.Commerce.Entities.Carts.Cart cart)
        {
            ManagerResponse<GetShippingOptionsResult, List<ShippingOption>> shippingPreferences = this.ShippingManager.GetShippingPreferences(cart);
            if (!shippingPreferences.ServiceProviderResult.Success)
            {
                jsonResult.SetErrors((ServiceProviderResult)shippingPreferences.ServiceProviderResult);
            }
            else
            {
                List<ShippingOption> list1 = shippingPreferences.ServiceProviderResult.ShippingOptions.ToList<ShippingOption>();
                List<LineShippingOption> list2 = shippingPreferences.ServiceProviderResult.LineShippingPreferences.ToList<LineShippingOption>();
                jsonResult.InitializeShippingOptions(list1);
                jsonResult.InitializeLineItemShippingOptions(list2);
            }
        }

        protected virtual void AddEmailShippingMethodToResult(
          CustomDeliveryDataJsonResult jsonResult,
          Sitecore.Commerce.Entities.Carts.Cart cart)
        {
            ShippingMethod emailShippingMethod1 = (ShippingMethod)null;
            ManagerResponse<GetShippingMethodsResult, ShippingMethod> emailShippingMethod2 = this.ShippingManager.GetEmailShippingMethod(this.StorefrontContext.CurrentStorefront, cart);
            if (emailShippingMethod2.ServiceProviderResult.Success)
                emailShippingMethod1 = emailShippingMethod2.Result;
            else
                jsonResult.SetErrors((ServiceProviderResult)emailShippingMethod2.ServiceProviderResult);
            jsonResult.InitializeEmailShippingMethod(emailShippingMethod1);
        }
      

        protected virtual void AddCustomAvailableCountries(BaseCheckoutDataJsonResult jsonResult)
        {
            ManagerResponse<GetAvailableCountriesResult, Dictionary<string, string>> availableCountries = this.OrderManager.GetAvailableCountries();
            Dictionary<string, string> strs = new Dictionary<string, string>();
            if (!availableCountries.ServiceProviderResult.Success)
            {
                jsonResult.SetErrors(availableCountries.ServiceProviderResult);
                return;
            }
            jsonResult.InitializeCountries(availableCountries.Result);
        }

        protected virtual void AddAvailableRegions(CustomDeliveryDataJsonResult jsonResult)
        {
            try
            {
                Dictionary<string, string> statesDictionary = new Dictionary<string, string>();
                foreach (Item child in this.GetRegionItem().GetChildren())
                    statesDictionary.Add(child[Sitecore.Commerce.Constants.Templates.Subdivision.Fields.Code], child[Sitecore.Commerce.Constants.Templates.Subdivision.Fields.Name]);
                jsonResult.States = statesDictionary;

            }
            catch (Exception ex)
            {
                jsonResult.SetErrors("find states area", ex);
            }
        }

        protected virtual Item GetRegionItem()
        {
            return Context.Database.GetItem("/sitecore/Commerce/Commerce Control Panel/Shared Settings/Countries-Regions/United States");
        }

       
    }
}
