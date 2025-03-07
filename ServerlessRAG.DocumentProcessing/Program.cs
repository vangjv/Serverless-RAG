using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerlessRAG.Unstructured;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();
// Retrieve the storage connection string from the environment variable.
// Ensure that "AzureWebJobsStorage" is set in your local.settings.json (or in your app settings in Azure)
string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");
builder.Services.AddSingleton(new BlobServiceClient(connectionString));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IIngestionService, IngestionService>();
builder.Build().Run();
