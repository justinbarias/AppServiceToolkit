### Getting started

To run functions locally, create a **local.settings.json** file with the following content:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "",
        "AzureWebJobsDashboard": "",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "ArmClientId": "<Service principal ID>",
        "ArmClientSecret": "<Service principal secret",
        "ArmSubscriptionId": "<Subscription ID>",
        "ArmTenantId": "<Tenant ID>",
        "storageConnectionString":"<Not used yet>"
    }
}
```