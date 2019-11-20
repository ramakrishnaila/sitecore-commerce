
using Sitecore.Commerce.XA.Foundation.Catalog.Managers;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Models;
using Sitecore.Commerce.XA.Foundation.Common.Providers;
using Sitecore.Commerce.XA.Foundation.Common.Utils;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;
using System;
using System.Globalization;
using System.Web;

namespace Custom.Commerce.Foundation.Catalog.Pipelines
{
    public class CatalogProductItemResolver : HttpRequestProcessor
    {
        public CatalogProductItemResolver()
        {
            this.SearchManager = ServiceLocatorHelper.GetService<ISearchManager>();
            Assert.IsNotNull((object)this.SearchManager, "this.SearchManager service could not be located.");
            this.StorefrontContext = ServiceLocatorHelper.GetService<IStorefrontContext>();
            Assert.IsNotNull((object)this.StorefrontContext, "this.StorefrontContext service could not be located.");
            this.ItemTypeProvider = ServiceLocatorHelper.GetService<IItemTypeProvider>();
            Assert.IsNotNull((object)this.ItemTypeProvider, "this.ItemTypeProvider service could not be located.");
            this.CatalogUrlManager = ServiceLocatorHelper.GetService<ICatalogUrlManager>();
            Assert.IsNotNull((object)this.CatalogUrlManager, "this.CatalogUrlManager service could not be located.");
            this.SiteContext = ServiceLocatorHelper.GetService<ISiteContext>();
            Assert.IsNotNull((object)this.SiteContext, "this.SiteContext service could not be located.");
            this.Context = ServiceLocatorHelper.GetService<IContext>();
            Assert.IsNotNull((object)this.SiteContext, "this.SitecoreContext service could not be located.");
        }
        public IContext Context { get; }

        public IStorefrontContext StorefrontContext { get; set; }

        public ISearchManager SearchManager { get; set; }

        public IItemTypeProvider ItemTypeProvider { get; set; }

        public ICatalogUrlManager CatalogUrlManager { get; set; }

        public ISiteContext SiteContext { get; set; }
        public override void Process(HttpRequestArgs args)
        {
            try
            {
            if (this.SiteContext.CurrentCatalogItem != null)
                return;
            Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes contextItemType = this.GetContextItemType(args.Url.ItemPath);

            switch (contextItemType)
            {
                case Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Category:
                case Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Product:
                    //get the catalog item id from sku passed in query string
                    string catalogItemIdFromUrl = this.GetCatalogItemIdFromUrl();
                    if (string.IsNullOrEmpty(catalogItemIdFromUrl))
                        break;
                    bool isProduct = contextItemType == Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Product;
                    string catalog = this.StorefrontContext.CurrentStorefront.Catalog;
                    Item obj = this.ResolveCatalogItem(catalogItemIdFromUrl, catalog, isProduct);
                    if (obj == null)
                        WebUtil.Redirect("~/");
                    this.SiteContext.CurrentCatalogItem = obj;
                    break;
            }
            }
            catch (Exception ex)
            {
                Diagnostics.Logger.Error("Error occured found for CatalogProductItemResolver method" + ex);
            }
        }

      //get the item id and search the item in sitecore commerce
      protected Item ResolveCatalogItem(
      string itemId,
      string catalogName,
      bool isProduct)
        {
            Item obj = (Item)null;
            if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(catalogName))
                obj = !isProduct ? this.SearchManager.GetCategory(itemId, catalogName) : this.SearchManager.GetProduct(itemId, catalogName);
            return obj;
        }
        private Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes GetContextItemType(string UrlItemPath)
        {
            //since this pipeline is being called before ite resolver, Sitecore.Context.Item has not been resolved, so we need to get the item by using item path.
            Item item = Sitecore.Context.Database.GetItem(UrlItemPath);
            if (item == null)
                return Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Unknown; 
            Template template = TemplateManager.GetTemplate(item);
            Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes itemTypes = Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Unknown;
            if (template.InheritsFrom(Sitecore.Commerce.XA.Foundation.Common.Constants.DataTemplates.CategoryPage.ID))
                itemTypes = Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Category;
            else if (template.InheritsFrom(Sitecore.Commerce.XA.Foundation.Common.Constants.DataTemplates.ProductPage.ID))
                itemTypes = Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Product;
            return itemTypes;
        }

        //this behaviour differes from sitecore Out of box behaviour of finding catalog item.Original method uses item id passed in query string ,
        //the following method uses SKU passed in query string to parse and get the sitecore commerce item.
        private string GetCatalogItemIdFromUrl()
        {
            if (this.IsGiftCardPageRequest())
                return this.StorefrontContext.CurrentStorefront.GiftCardProductId;
            string commerceItemId = string.Empty;
            string rawUrl = HttpContext.Current.Request.RawUrl;
            int num = rawUrl.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            if (num > 0)
            {
                //get the sku of the product passed in query string
                string skuOfItem = rawUrl.Substring(num + 1);
                //find the sitecore item using 
                CommerceStorefront currentStorefront = StorefrontContext.CurrentStorefront;
				//get the sitecore commerce catalog item id from SKU which is passed in the query string.replace GetCustomCatalogItemIdBySku with your customization 
                commerceItemId =  GetCustomCatalogItemIdBySku(skuOfItem, currentStorefront, SearchManager);
            }
            return commerceItemId;
        }


        protected virtual bool IsGiftCardPageRequest()
        {
            bool? flag;
            if (this.IsGiftCardProductPage)
            {
                flag = true;
            }
            else
            {
                string lowerInvariant = this.Context.Language.ToString().ToLowerInvariant();
                string str = HttpContext.Current.Request.Url.AbsolutePath.ToLowerInvariant().Replace(".aspx", string.Empty);
                if (str.Contains(lowerInvariant))
                    str = str.Replace("/" + lowerInvariant, string.Empty);
                flag = this.StorefrontContext.CurrentStorefront.GiftCardPageLink?.ToLowerInvariant().Replace(".aspx", string.Empty).EndsWith(str, StringComparison.OrdinalIgnoreCase);
                this.IsGiftCardProductPage = flag.HasValue ? flag.Value : false;
            }
            return flag.HasValue ? flag.Value : false;
        }
        private bool IsGiftCardProductPage
        {
            get
            {
                return System.Convert.ToBoolean(HttpContext.Current.Items[(object)nameof(IsGiftCardProductPage)], (IFormatProvider)CultureInfo.InvariantCulture);
            }
            set
            {
                HttpContext.Current.Items[(object)nameof(IsGiftCardProductPage)] = (object)value;
            }
        }
    }
}
