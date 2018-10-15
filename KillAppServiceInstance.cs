
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
    public static class KillAppServiceInstance
    {

        [FunctionName("KillAppServiceInstanceManual")]
        public static async Task<IActionResult> KillAppServiceInstanceForInstanceId([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "KillAppServiceInstanceManual")]HttpRequest req, ILogger log, ExecutionContext context)
        {
            
            log.LogInformation("C# HTTP trigger function processed a request.");
           
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = config.Build();

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(configuration["ArmClientId"],configuration["ArmClientSecret"],configuration["armTenantId"],AzureEnvironment.AzureGlobalCloud);
            
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var data = JsonConvert.DeserializeObject<AppServiceInstanceKillPayload>(requestBody);
            
            string instanceId = data?.instanceId;
            string resourceGroupName = data?.resourceGroupName;
            string webAppName = data?.resourceName;
            var res = false;
            
            try {
                log.LogInformation("Authenticating against Azure!");
                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(configuration["ArmSubscriptionId"]);
                    

                log.LogInformation("Killing process!");
                AzureWebAppService service = new AzureWebAppService(azure, log, null);
                res = await service.KillAppServiceForInstanceId(resourceGroupName, webAppName, instanceId);

            }
            catch(Exception e)
            {
                log.LogError(e, "Error executing task, details = " + e.Message);
            }

            return data != null
                ? (ActionResult)new OkObjectResult(
                    new AppServiceInstanceKillPayloadResponse {
                        resourceGroupName = data.resourceGroupName,
                        resourceName = data.resourceName,
                        instanceId = data.instanceId,
                        isSucceeded = res
                    }
                )
                : new BadRequestObjectResult("Request body invalid");
        
        }
        [FunctionName("KillAppServiceInstance")]
        public static IActionResult KillAppServiceInstanceAuto([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "KillAppServiceInstance")]HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
           
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
            bool result = false;
            
            try {
                log.LogInformation("Authenticating against Azure!");
                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(configuration["ArmSubscriptionId"]);
                    

                log.LogInformation("Killing process!");
                AzureWebAppService service = new AzureWebAppService(azure, log, null);
                var res = service.KillAppServiceProcess(resourceGroupName, webAppName, serverName);
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