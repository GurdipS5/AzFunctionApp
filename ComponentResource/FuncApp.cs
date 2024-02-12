using Pulumi;
using Pulumi.AzureNative.Storage;
using p = Pulumi;

namespace Kinderworx.Pulumi.ComponentResources.AzFuncApp
{
    public class FuncApp : p.ComponentResource
    {
        public FuncApp(string type, string resourceGroupName,  string location, string name, ResourceArgs? args, ComponentResourceOptions? options = null, bool remote = false) : base(type, name, args, options, remote)
        {
            var exampleAccount = new p.AzureNative.Storage.StorageAccount("exampleAccount", new()
            {
                ResourceGroupName = resourceGroupName,
                Location = location,
                AccessTier = AccessTier.Premium,
                Kind = "LRS",

                AccountTier = "Standard",
                AccountReplicationType = "LRS",
            });

            var examplePlan = new p.AzureNative.AppService.Plan("examplePlan", new()
            {
                Location = exampleResourceGroup.Location,
                ResourceGroupName = exampleResourceGroup.Name,
                Sku = new Azure.AppService.Inputs.PlanSkuArgs
                {
                    Tier = "Standard",
                    Size = "S1",
                },
            });

            var exampleFunctionApp = new p.AzureNative.AppService.FunctionApp("exampleFunctionApp", new()
            {
                Location = exampleResourceGroup.Location,
                ResourceGroupName = exampleResourceGroup.Name,
                AppServicePlanId = examplePlan.Id,
                StorageAccountName = exampleAccount.Name,
                StorageAccountAccessKey = exampleAccount.PrimaryAccessKey,
            });
        }
    }
}
