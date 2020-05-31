using Microsoft.OData.Client;
using Sitecore.Commerce.Engine.Connect.Pipelines;
using Sitecore.Commerce.Engine.Connect.Pipelines.Arguments;
using Sitecore.Commerce.Pipelines;
using Sitecore.Commerce.ServiceProxy;
using Sitecore.Commerce.Services;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using System;
using System.Web.Mvc;

namespace LXL.Feature.Cart.Pipelines
{
    public class GetClientToken : PipelineProcessor
    {
        IAccountManager _accountManager = null;
        public override void Process(ServicePipelineArgs args)
        {
            ServiceProviderRequest request;
            PaymentClientTokenResult result;
            PipelineUtility.ValidateArguments<ServiceProviderRequest, PaymentClientTokenResult>(args, out request, out result);
            try
            {
                string commerceCustomerId = string.Empty;
                _accountManager = DependencyResolver.Current.GetService<IAccountManager>();
                var user = this._accountManager.GetUser(Sitecore.Context.GetUserName());
                commerceCustomerId = user?.Result?.ExternalId;
                string str = (string)Proxy.GetValue<string>(this.GetContainer(request.Shop.Name, string.Empty,commerceCustomerId, string.Empty, args.Request.CurrencyCode, new DateTime?(), "").GetClientToken());
                if (str != null)
                    result.ClientToken = str;
                else
                    result.Success = false;
            }
            catch (ArgumentException ex)
            {
                result.Success = false;
                result.SystemMessages.Add(PipelineUtility.CreateSystemMessage((Exception)ex));
            }
            base.Process(args);
        }
    }
}
