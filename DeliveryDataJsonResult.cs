using Sitecore.Commerce.Entities.Shipping;
using Sitecore.Commerce.XA.Feature.Cart.Models.JsonResults;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Connect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace  Sitecore.Commerce.XA.Feature.Cart.Models.JsonResults
{
  public class CustomDeliveryDataJsonResult : CustomBaseCheckoutDataJsonResult
    {
    public CustomDeliveryDataJsonResult(
      IStorefrontContext storefrontContext,
      IModelProvider modelProvider,
      IContext context)
      : base(storefrontContext, modelProvider, context)
    {
    }

    public IEnumerable<ShippingOptionJsonResult> OrderShippingOptions { get; set; }

    public IEnumerable<LineShippingOptionJsonResult> LineShippingOptions { get; set; }

    public ShippingMethodJsonResult EmailDeliveryMethod { get; set; }
    public IDictionary<string, string> States { get; set; }
    public override void Initialize(Sitecore.Commerce.Entities.Carts.Cart cart, IVisitorContext visitorContext)
    {
      this.CurrencyCode = this.StorefrontContext.CurrentStorefront.SelectedCurrency;
      base.Initialize(cart, visitorContext);
    }

    public virtual void InitializeShippingOptions(List<ShippingOption> shippingOptions)
    {
      if (shippingOptions == null)
        return;
      List<ShippingOptionJsonResult> optionJsonResultList = new List<ShippingOptionJsonResult>();
      foreach (ShippingOption shippingOption in shippingOptions)
      {
        ShippingOptionJsonResult model = this.ModelProvider.GetModel<ShippingOptionJsonResult>();
        model.Initialize(shippingOption);
        optionJsonResultList.Add(model);
      }
      this.OrderShippingOptions = (IEnumerable<ShippingOptionJsonResult>) optionJsonResultList;
    }

    public virtual void InitializeLineItemShippingOptions(
      List<LineShippingOption> lineShippingOptions)
    {
      if (lineShippingOptions == null || !lineShippingOptions.Any<LineShippingOption>())
        return;
      List<LineShippingOptionJsonResult> source = new List<LineShippingOptionJsonResult>();
      foreach (LineShippingOption lineShippingOption in lineShippingOptions)
      {
        LineShippingOptionJsonResult model = this.ModelProvider.GetModel<LineShippingOptionJsonResult>();
        model.Initialize(lineShippingOption);
        source.Add(model);
      }
      this.LineShippingOptions = (IEnumerable<LineShippingOptionJsonResult>) source;
      foreach (CartLineJsonResult line1 in this.Cart.Lines)
      {
        CartLineJsonResult line = line1;
        LineShippingOptionJsonResult optionJsonResult = source.FirstOrDefault<LineShippingOptionJsonResult>((Func<LineShippingOptionJsonResult, bool>) (l => l.LineId.Equals(line.ExternalCartLineId, StringComparison.OrdinalIgnoreCase)));
        if (optionJsonResult != null)
          line.SetShippingOptions(optionJsonResult.ShippingOptions);
      }
    }

    public virtual void InitializeEmailShippingMethod(ShippingMethod emailShippingMethod)
    {
      if (emailShippingMethod == null)
        return;
      this.EmailDeliveryMethod = this.ModelProvider.GetModel<ShippingMethodJsonResult>();
      this.EmailDeliveryMethod.Initialize(emailShippingMethod);
    }
    
  }
}
