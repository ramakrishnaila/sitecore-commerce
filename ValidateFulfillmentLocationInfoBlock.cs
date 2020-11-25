using Aperture.Sitecore.Commerce.Plugin.InRiverConnector.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace HI.Site.Commerce.Huco.Plugin.CartLine.Extensions.Pipelines.Blocks
{
    [PipelineDisplayName("HI.Site.Commerce.Huco.Plugin.CartLine.Extensions.Pipelines.Blocks.GetCartLineArticleInfoBlock")]
    public class ValidateFulfillmentLocationInfoBlock : PipelineBlock<CartLineArgument, CartLineArgument, CommercePipelineExecutionContext>
    {
        private readonly IGetCartPipeline _getCartPipeline;
        private readonly IGetSellableItemPipeline _getSellableItemPipeline;
        public ValidateFulfillmentLocationInfoBlock(IGetCartPipeline getCartPipeline, IGetSellableItemPipeline getSellableItemPipeline)
        : base(null)
        {
            _getSellableItemPipeline = getSellableItemPipeline;
            _getCartPipeline = getCartPipeline;
        }

        public override async Task<CartLineArgument> Run(CartLineArgument arg, CommercePipelineExecutionContext context)
        {
            string fulfillmentLocationCart = string.Empty;
            Condition.Requires(arg).IsNotNull("GetCartLineArticleInfoBlock: Cart can not be null");
            var currentSellableItem = context.CommerceContext.GetEntity<SellableItem>();
            var currentCartLineArg = context.CommerceContext.GetObject<CartLineArgument>();
            Condition.Requires(currentSellableItem).IsNotNull("GetCartLineArticleInfoBlock: Sellable item can not be null");
            SellableItemService sellableItemService = new SellableItemService(_getSellableItemPipeline);
            string fulfillmentLocationItem = sellableItemService.GetFulFilmmentLocationForItem(currentSellableItem);
            //get the current cart 
            if (!string.IsNullOrEmpty(context.CommerceContext?.Headers["CustomerId"]))
            {
                string cartId = "Default" + context.CommerceContext?.Headers["CustomerId"] + context.CommerceContext.CurrentShopName();
                var resolveCartArgument = new ResolveCartArgument(context.CommerceContext.CurrentShopName(),
                                                     cartId,
                                                     context.CommerceContext.CurrentShopperId());
                Cart cart = await this._getCartPipeline.Run(resolveCartArgument, context.CommerceContext.PipelineContextOptions);

                if (cart != null && cart.Lines != null && cart.Lines.Count > 0)
                {
                    fulfillmentLocationCart = await sellableItemService.GetFulFilmmentLocationForCartLines(cart.Lines, context);
                }
                        
                CommercePipelineExecutionContext executionContext;
                if (cart.Lines.Count > 0 && fulfillmentLocationCart != fulfillmentLocationItem)
                {
                    executionContext = context;
                    CommerceContext commerceContext = context.CommerceContext;
                    string error = context.GetPolicy<KnownResultCodes>().Error;
                    object[] args = new object[1]
                    {
                     (object) currentSellableItem.DisplayName
                    };
                    string defaultMessage = "Item '" + currentSellableItem.DisplayName + "' is not purchasable.";
                    executionContext.Abort(await commerceContext.AddMessage(error, "AddCartFulfillmentErrorMessage", args, defaultMessage).ConfigureAwait(false), (object)context);
                    return (CartLineArgument)null;
                }
            }
            return arg;
        }
    }
}
