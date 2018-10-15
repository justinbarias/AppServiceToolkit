using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// Metrics Payload schema described here: https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/monitoring-near-real-time-metric-alerts#payload-schema
namespace MSFT.AppServiceToolkit {

    public class AppServiceInstanceKillPayload {
        public string instanceId;
        public string resourceGroupName;
        public string resourceName;
    }

    public class AppServiceInstanceKillPayloadResponse {
        public string instanceId;
        public string resourceGroupName;
        public string resourceName;
        public bool isSucceeded;
    }
}