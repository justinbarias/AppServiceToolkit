using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// Metrics Payload schema described here: https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/monitoring-near-real-time-metric-alerts#payload-schema
namespace MSFT.AppServiceToolkit {

    public class AppServicesMetricDimension {
        public string name;
        public string value;
    }
    public class AppServicesMetricInfo {
        public string metricName;
        public List<AppServicesMetricDimension> dimensions;
        [JsonProperty("operator")]
        public string metricOperator;
        public string threshold;
        public string timeAggregation;
        public float metricValue;
    }
    public class AppServicesMetricPayloadCondition {
        public string windowSize;
        public List<AppServicesMetricInfo> allOf;
    }

    public class AppServicesMetricPayloadContext {
        public DateTime timestamp;
        public string id;
        public string alertName;
        public string description;
        public string conditionType;

        public AppServicesMetricPayloadCondition condition;
        public string subscriptionId;
        public string resourceGroupName;
        public string resourceName;
        public string resourceType;
        public string resourceId;
        public string portalLink;

    }
    public class AppServicesMetricPayloadData {
        public string version;
        public string status;
        public AppServicesMetricPayloadContext context;
    }
    public class AppServicesMetricPayload {
        public string schemaId;
        public AppServicesMetricPayloadData data;
    }
}