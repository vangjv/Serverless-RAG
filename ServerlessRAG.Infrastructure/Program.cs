using System.Diagnostics;
class Program
{
    static async Task Main(string[] args)
    {
        //string clientId = "<YOUR_CLIENT_ID>";
        //string clientSecret = "<YOUR_CLIENT_SECRET>";
        //string tenantId = "<YOUR_TENANT_ID>";
        //string loginCommand = $"az login --service-principal -u {clientId} -p {clientSecret} --tenant {tenantId}";
        //ExecuteAzureCliCommand(loginCommand);
        string resourceGroup = "serverlessragdemo";
        string location = "centralus";
        string documentProcessorFunctionAppStorage = "serverlessragdemo";
        string documentProcessorFunctionAppName = "serverlessragdemo";
        string lanceDbFunctionAppStorage = "serverlesslancedbdemo";
        string lanceDbFunctionFunctionAppName = "serverlesslancedbdemo";

        //single command
        //await ExecuteAzureCliCommandAsync($"az functionapp create --resource-group {resourceGroup} --consumption-plan-location {location} --runtime dotnet-isolated --runtime-version 8 --functions-version 4 --name {documentProcessorFunctionAppName} --storage-account {documentProcessorFunctionAppStorage}");

        //create resources group
        //await ExecuteAzureCliCommandAsync($"az group create --name {resourceGroup} --location {location}");

        //await CreateServerlessRagFunctionApp(resourceGroup,location, documentProcessorFunctionAppStorage, documentProcessorFunctionAppName);
        //await CreateServerlessLanceDbFunctionApp(resourceGroup, location, lanceDbFunctionAppStorage, lanceDbFunctionFunctionAppName);
        
    }

    static async Task CreateServerlessRagFunctionApp (string resourceGroup, string location, string documentProcessorFunctionAppStorage, string documentProcessorFunctionAppName)
    {
        List<string> commands = new List<string>
        {
            "az account show",
            $"az storage account create --name {documentProcessorFunctionAppStorage} --resource-group {resourceGroup} --location {location} --sku Standard_LRS",
            $"az functionapp create --resource-group {resourceGroup} --consumption-plan-location {location} --runtime dotnet-isolated --runtime-version 8 --functions-version 4 --name {documentProcessorFunctionAppName} --storage-account {documentProcessorFunctionAppStorage}",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings BlobStorageConnString=",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings Unstructured:ApiUrl=https://api.unstructuredapp.io/general/v0/general",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings Unstructured:ApiKey=",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings Unstructured:Strategy=fast",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings VoyageAPIKey=",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings VoyageEmbeddingModel=voyage-3-large",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings OpenAIAPIKey=",
            $"az functionapp config appsettings set --name {documentProcessorFunctionAppName} --resource-group {resourceGroup} --settings PdfPagesPerSection=15"
        };

        //use this to allow deploying from visual studio
        //commands.Add($"az resource update --resource-group {resourceGroup} --name scm --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies --parent sites/{documentProcessorFunctionAppName} --set properties.allow=false");

        //enable cors from all origins
        //first remove any existing origins
        commands.Add($"az functionapp cors remove --allowed-origins --name {documentProcessorFunctionAppName} --resource-group {resourceGroup}");
        commands.Add($"az functionapp cors add --allowed-origins \"*\" --name {documentProcessorFunctionAppName} --resource-group {resourceGroup}");
        foreach (var cmd in commands)
        {
            try
            {
                Console.WriteLine($"Executing command '{cmd}");
                await ExecuteAzureCliCommandAsync(cmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command '{cmd}': {ex.Message}");
            }
        }
    }

    static async Task CreateServerlessLanceDbFunctionApp(string resourceGroup, string location, string lanceDbFunctionAppStorage, string lanceDbFunctionFunctionAppName)
    {

        List<string> commands = new List<string>
        {
            //$"az storage account create --name {lanceDbFunctionAppStorage} --resource-group {resourceGroup} --location {location} --sku Standard_LRS",
            $"az functionapp create --name {lanceDbFunctionFunctionAppName} --storage-account {lanceDbFunctionAppStorage} --consumption-plan-location {location} --resource-group {resourceGroup} --os-type Linux --runtime python --runtime-version 3.9 --functions-version 4",
            $"az functionapp config appsettings set --name {lanceDbFunctionFunctionAppName} --resource-group {resourceGroup} --settings LanceDbContainerFolderURI=az://lancedb/database",
            $"az functionapp config appsettings set --name {lanceDbFunctionFunctionAppName} --resource-group {resourceGroup} --settings StorageAccountKey=",
            $"az functionapp config appsettings set --name {lanceDbFunctionFunctionAppName} --resource-group {resourceGroup} --settings StorageAccountName=serverlesslancedb",
        };

        foreach (var cmd in commands)
        {
            try
            {
                Console.WriteLine($"Executing command '{cmd}");
                await ExecuteAzureCliCommandAsync(cmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command '{cmd}': {ex.Message}");
            }
        }
    }

    static async Task ExecuteAzureCliCommandAsync(string command)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/C " + command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processInfo))
        {
            using (StreamReader outputReader = process.StandardOutput)
            using (StreamReader errorReader = process.StandardError)
            {
                string output = await outputReader.ReadToEndAsync();
                string error = await errorReader.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error: {error}");
                }
            }
        }
    }
}
