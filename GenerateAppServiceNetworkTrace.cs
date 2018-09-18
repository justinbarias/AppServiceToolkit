

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace MSFT.AppServiceToolkit
{
    public static class GenerateAppServiceNetworkTrace
    {
        [FunctionName("GenerateAppServiceNetworkTrace")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = config.Build();

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(configuration["ArmClientId"],configuration["ArmClientSecret"],configuration["armTenantId"],AzureEnvironment.AzureGlobalCloud);
            
            

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var data = JsonConvert.DeserializeObject<AppServicesMetricPayload>(requestBody);
            var name =data?.data.context.resourceName;
            
            string resourceGroupName = data?.data.context.resourceGroupName;
            string webAppName = data?.data.context.resourceName;
            //string processId = data?.data.context.condition.allOf.FirstOrDefault().dimensions.Where(dim => dim.name == "ResourceId").FirstOrDefault().value;
            string serverName = data?.data.context.condition.allOf.FirstOrDefault().dimensions.Where(dim => dim.name == "ServerName").FirstOrDefault().value;
            string result = "";
            
            try {
                log.LogInformation("Authenticating against Azure!");
                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(configuration["ArmSubscriptionId"]);

                log.LogInformation("Generating network trace!");
                AzureWebAppService service = new AzureWebAppService(azure, log, null);
                var res = service.StartNetworkTrace(resourceGroupName, webAppName, serverName, "https://cbafuncmetricsacea.blob.core.windows.net/?sv=2017-11-09&ss=bfqt&srt=sco&sp=rwdlacup&se=2018-09-30T12:22:33Z&st=2018-09-16T04:22:33Z&spr=https&sig=HC0Wuu0J2k%2BiofNqm3TEoXqBXTkLZ4HKKBllvUpmq88%3D");
                Task.WaitAll(res);
                result = res.Result;
            }
            catch(Exception e)
            {
                log.LogError(e, "Error creating ARM Client, details = " + e.Message);
            }
             
            log.LogInformation($"{data}");

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}. Your settings are {resourceGroupName},{webAppName},{serverName}. Task returned {result}.")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        
        }
    }
}
