using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;


namespace MSFT.AppServiceToolkit
{
    public class AzureWebAppService : IAzureWebAppService
    {

        private readonly IAzure _client;
        private readonly ILogger _logger;
        private readonly CloudBlobClient _blobClient;

        public AzureWebAppService(IAzure client, ILogger logger, CloudBlobClient blobClient)
        {
            this._client = client;
            this._logger = logger;
            this._blobClient = blobClient;
        }

        public async Task<bool> KillAppServiceProcess(string resourceGroupName, string webAppName, string serverName)
        {


            var validResult = await getInstanceProcessPair(resourceGroupName, webAppName, serverName);
            if (validResult != null)
            {
                _logger.LogInformation($"Deleting process instance {validResult.processId} for instance {validResult.instanceId}");
                var task = _client.WebApps.Inner.DeleteInstanceProcessAsync(resourceGroupName, webAppName, validResult.processId, validResult.instanceId);

                await task;

                return task.IsCompleted;

            }
            else
            {
                _logger.LogError($"Valid instance not found for webApp {webAppName} and serverName {serverName}");
                return false;
            }
        }

        public class InstanceProcessPair
        {
            public string instanceId;
            public string processId;
        }

        private async Task<InstanceProcessPair> getInstanceProcessPair(string resourceGroupName, string webAppName, string serverName)
        {
            _logger.LogInformation($"Getting instances list");
            var getInstanceId = await _client.WebApps.Inner.ListInstanceIdentifiersAsync(resourceGroupName, webAppName);
            _logger.LogInformation($"Found {getInstanceId.Count()} instances");

            var instanceProcessObject = getInstanceId.Select(
                async (instance) =>
                {
                    _logger.LogInformation($"Retrieving process list for instance {instance.Name}");
                    var processList = await _client.WebApps.Inner.ListInstanceProcessesAsync(resourceGroupName, webAppName, instance.Name);
                    _logger.LogInformation($"Retrieved {processList.Count()} processes from {instance.Name}");
                    var processId = processList.Where(p =>
                    {
                        var tokens = p.Name.Split('/');
                        var pid = tokens.ElementAt(tokens.Length - 1);
                        var processDetail = _client.WebApps.Inner.GetInstanceProcessAsync(resourceGroupName, webAppName, pid, instance.Name);
                        Task.WaitAll(processDetail);
                        var isScmSite = processDetail.Result.IsScmSite ?? false;
                        //if COMPUTERNAME exists return actual process computername, else return invalid
                        var computerName = processDetail.Result.EnvironmentVariables.ContainsKey("COMPUTERNAME") ? processDetail.Result.EnvironmentVariables["COMPUTERNAME"] : "invalid";
                       foreach(var key in processDetail.Result.EnvironmentVariables.Keys) {
                           _logger.LogInformation($"List of Environment Variables: {key}");
                       }
                        
                        return (!isScmSite && processDetail.Result.FileName.Contains("w3wp") && computerName == serverName);
                    }


                    ).FirstOrDefault()?.Id;

                    if(processId != null){
                        var pidTokens = processId.Split('/');
                        var pidActual = pidTokens.ElementAt(pidTokens.Length - 1);
                        if (pidActual == null)
                        {
                            _logger.LogInformation($"Not valid instance for instance {instance.Name} for serverName {serverName}");
                            return null;
                        }
                        else
                        {
                            _logger.LogInformation($"Found process {pidActual} w3wp for instance {instance.Name} with serverName {serverName}");
                            return new InstanceProcessPair
                            {
                                instanceId = instance.Name,
                                processId = pidActual
                            };
                        }
                    }
                    else {
                        _logger.LogInformation($"Not process found for instance {instance.Name} for serverName {serverName}");
                        return null;
                    }
                }
            ).Where(ipo => ipo.Result != null).FirstOrDefault();

            return instanceProcessObject.Result;
        }

        public async Task<string> GenerateProcessInstanceDump(string resourceGroupName, string webAppName, string serverName, string sasUrl)
        {
            var validResult = await getInstanceProcessPair(resourceGroupName, webAppName, serverName);
            CloudBlobContainer cloudBlobContainer = null;
            // this requires AlwaysOn to be turned on
            try
            {
                var task = await _client.WebApps.Inner.GetInstanceProcessDumpAsync(resourceGroupName, webAppName, validResult.processId, validResult.instanceId);
                var webApp =  await _client.WebApps.GetByIdAsync("asd");
                 _logger.LogInformation($"Initialising blob container");
                cloudBlobContainer = _blobClient.GetContainerReference("appservicememorydump" + Guid.NewGuid().ToString());
                await cloudBlobContainer.CreateAsync();

                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await cloudBlobContainer.SetPermissionsAsync(permissions);

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference("appservicememorydump.dmp");
                _logger.LogInformation($"Uploading memory dump to blob {cloudBlockBlob.StorageUri}");
                await cloudBlockBlob.UploadFromStreamAsync(task);
                return "success";
            }
            catch (Microsoft.Rest.Azure.CloudException e)
            {
                _logger.LogError($"Error generating network trace. Message {e.Message}, Data {e.Response.Content} ");
                return "";
            }
        }

        public async Task<string> StartNetworkTrace(string resourceGroupName, string webAppName, string serverName, string sasUrl)
        {
            try
            {
                var task = await _client.WebApps.Inner.StartWebSiteNetworkTraceAsync(resourceGroupName, webAppName);

                return task;
            }
            catch (Microsoft.Azure.Management.AppService.Fluent.Models.DefaultErrorResponseException e)
            {
                _logger.LogError($"Error generating network trace. Message {e.Message}, Data {e.Response.Content} ");
                return "";
            }
        }
        public async Task<string> StopNetworkTrace(string resourceGroupName, string webAppName)
        {
            try
            {
                var task = await _client.WebApps.Inner.StopWebSiteNetworkTraceAsync(resourceGroupName, webAppName);

                return task;
            }
            catch (Microsoft.Azure.Management.AppService.Fluent.Models.DefaultErrorResponseException e)
            {
                _logger.LogError($"Error generating network trace. Message {e.Message}, Data {e.Response.Content} ");
                return "";
            }
        }
    }
}
