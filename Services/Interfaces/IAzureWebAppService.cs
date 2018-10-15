using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Http;
using Newtonsoft.Json;



namespace MSFT.AppServiceToolkit {
    public interface IAzureWebAppService {

        Task<bool> KillAppServiceProcess(string resourceGroupName, string webAppName, string serverName);

        Task<bool> KillAppServiceForInstanceId(string resourceGroupName, string webAppName, string instanceId);

        Task<string> GenerateProcessInstanceDump(string resourceGroupName, string webAppName, string serverName, string sasUrl);

        Task<string> StartNetworkTrace(string resourceGroupName, string webAppName, string serverName, string sasUrl);
        Task<string> StopNetworkTrace(string resourceGroupName, string webAppName);

    }
}