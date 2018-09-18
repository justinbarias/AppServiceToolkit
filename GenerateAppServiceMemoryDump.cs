

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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;


namespace MSFT.AppServiceToolkit
{
    public static class GenerateAppServiceMemoryDump
    {
        [FunctionName("GenerateAppServiceMemoryDump")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = config.Build();
            CloudStorageAccount storageAccount = null;
            CloudBlobClient cloudBlobClient = null;
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(configuration["ArmClientId"],configuration["ArmClientSecret"],configuration["armTenantId"],AzureEnvironment.AzureGlobalCloud);
            
            var storageConnectionString = configuration["storageConnectionString"];

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

                if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
                {
                    cloudBlobClient = storageAccount.CreateCloudBlobClient();
                }
                else {
                    throw new Exception("Could not initiliase CloudBlobClient");
                }

                log.LogInformation("Generating memory dump trace!");
                AzureWebAppService service = new AzureWebAppService(azure, log, cloudBlobClient);
                var res = service.GenerateProcessInstanceDump(resourceGroupName, webAppName, serverName, "https://cbafuncmetricsacea.blob.core.windows.net/networktracedump/appservicememorydump.dmp");
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
