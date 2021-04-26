using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Services.AppAuthentication;

namespace Company.Function
{
    public class DatabaseAccountListKeysResult
    {
        public string primaryMasterKey {get;set;}
        public string primaryReadonlyMasterKey {get; set;}
        public string secondaryMasterKey {get; set;}
        public string secondaryReadonlyMasterKey {get;set;}
    }

    public static class HttpTriggerCSharp1
    {
        [FunctionName("HttpTriggerCSharp1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string keyMURI = System.Environment.GetEnvironmentVariable($"CosmosKeysEndpoint");
            string cosmosDBEndpoint = System.Environment.GetEnvironmentVariable($"cosmosDBEndpoint");
            string cosmosDBName = System.Environment.GetEnvironmentVariable($"cosmosDBName");
            string cosmosConName = System.Environment.GetEnvironmentVariable($"cosmosConName");

            // AzureServiceTokenProvider will help us to get the Service Managed token.
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            // Authenticate to the Azure Resource Manager to get the Service Managed token.
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

            log.LogInformation("Retreived Cosmos key management endpoint: " + keyMURI);

            // Setup an HTTP Client and add the access token.
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Post to the endpoint to get the keys result.
            var result = await httpClient.PostAsync(keyMURI, new StringContent(""));

            // Get the result back as a DatabaseAccountListKeysResult.
            DatabaseAccountListKeysResult keys = await result.Content.ReadAsAsync<DatabaseAccountListKeysResult>();
            log.LogInformation("Read only key: " + keys.primaryReadonlyMasterKey);

            log.LogInformation("Starting to create the client");
            CosmosClient client = new CosmosClient(cosmosDBEndpoint, keys.primaryReadonlyMasterKey);

            log.LogInformation("Client created");
            var database = client.GetDatabase(cosmosDBName);
            var container = database.GetContainer(cosmosConName);

            log.LogInformation("Connected to db/container: " + database + "/" + container);



            return new OkObjectResult("Success");
        }
    }
}
