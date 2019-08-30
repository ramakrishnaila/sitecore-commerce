using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Custom.Commerce.Plugin.Fulfillment
{
  [PipelineDisplayName("Fulfillment.block.FilterCartFulfillmentOptionsBlock")]
  public class FilterCartFulfillmentOptionsBlock : PipelineBlock<CartArgument, IEnumerable<FulfillmentOption>, CommercePipelineExecutionContext>
  {
    private readonly IGetFulfillmentOptionsPipeline _getOptions;

    public FilterCartFulfillmentOptionsBlock(IGetFulfillmentOptionsPipeline getOptionsPipeline)
      : base((string) null)
    {
      this._getOptions = getOptionsPipeline;
    }

    public override async Task<IEnumerable<FulfillmentOption>> Run(
      CartArgument arg,
      CommercePipelineExecutionContext context)
    {
      Condition.Requires<CartArgument>(arg).IsNotNull<CartArgument>("The arg can not be null");
      Condition.Requires<Cart>(arg.Cart).IsNotNull<Cart>("The cart can not be null");
      Cart cart = arg.Cart;
      if (!cart.Lines.Any<CartLineComponent>())
      {
        CommercePipelineExecutionContext executionContext = context;
        CommerceContext commerceContext = context.CommerceContext;
        string validationError = context.CommerceContext.GetPolicy<KnownResultCodes>().ValidationError;
        object[] args = new object[1]{ (object) cart.Id };
        string defaultMessage = "Cart '" + cart.Id + "' has no lines";
        executionContext.Abort(await commerceContext.AddMessage(validationError, "CartHasNoLines", args, defaultMessage), (object) context);
        executionContext = (CommercePipelineExecutionContext) null;
        return new List<FulfillmentOption>().AsEnumerable<FulfillmentOption>();
      }
      List<FulfillmentOption> list = (await this._getOptions.Run(string.Empty, context)).ToList<FulfillmentOption>();
      if (list.Any<FulfillmentOption>() && cart.Lines.Count == 1)
      {
        FulfillmentOption fulfillmentOption = list.FirstOrDefault<FulfillmentOption>((Func<FulfillmentOption, bool>) (o => o.FulfillmentType.Equals("SplitShipping", StringComparison.OrdinalIgnoreCase)));
        if (fulfillmentOption != null)
          list.Remove(fulfillmentOption);
      }
        bool cartHasDigitalProducts = false;
        bool cartHasTangibleProducts = false;
      foreach (CartLineComponent withSubLine in (IEnumerable<CartLineComponent>) arg.Cart.Lines.WithSubLines())
      {
        if (!withSubLine.CartSubLineComponents.Any<CartLineComponent>())
          {
                    //if (withSubLine.GetComponent<CartProductComponent>().HasPolicy<AvailabilityAlwaysPolicy>())
                    if (withSubLine.GetComponent<CartProductComponent>().Tags.Any<Tag>((Func<Tag, bool>)(p => p.Name.Equals("digitalsubscription", StringComparison.OrdinalIgnoreCase))))
                    {
                        cartHasDigitalProducts = true;
                    }
                    else
                    {
                        cartHasTangibleProducts = true;
                    }
                    }
       }
          //cart has digital products only
          if(cartHasDigitalProducts && !cartHasTangibleProducts)
            {
                //lets remove the shiptome option
                if (list.Any<FulfillmentOption>((Func<FulfillmentOption, bool>)(p => p.FulfillmentType.Equals("ShipToMe", StringComparison.OrdinalIgnoreCase))))
                  list.Remove(list.First<FulfillmentOption>((Func<FulfillmentOption, bool>) (p => p.FulfillmentType.Equals("ShipToMe", StringComparison.OrdinalIgnoreCase))));

                if (list.Any<FulfillmentOption>((Func<FulfillmentOption, bool>)(p => p.Name.ToLower().Equals("shiptome", StringComparison.OrdinalIgnoreCase))))
                    list.Remove(list.First<FulfillmentOption>((Func<FulfillmentOption, bool>)(p => p.Name.ToLower().Equals("shiptome", StringComparison.OrdinalIgnoreCase))));

            }
            if (!cartHasDigitalProducts && cartHasTangibleProducts)
            {
                //lets remove the digital products option
                if (list.Any<FulfillmentOption>((Func<FulfillmentOption, bool>)(p => p.FulfillmentType.Equals("Digital", StringComparison.OrdinalIgnoreCase))))
                list.Remove(list.First<FulfillmentOption>((Func<FulfillmentOption, bool>) (p => p.FulfillmentType.Equals("Digital", StringComparison.OrdinalIgnoreCase))));

                if (list.Any<FulfillmentOption>((Func<FulfillmentOption, bool>)(p => p.Name.ToLower().Equals("digital", StringComparison.OrdinalIgnoreCase))))
                    list.Remove(list.First<FulfillmentOption>((Func<FulfillmentOption, bool>)(p => p.Name.ToLower().Equals("digital", StringComparison.OrdinalIgnoreCase))));

            }
            //if cart has digital products and tangible products, Do not remove them from list.
            return list.AsEnumerable<FulfillmentOption>();
    }
  }
}
