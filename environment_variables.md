//serverlessrag environment variables
{
    "name": "OpenAIAPIKey",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "PdfPagesPerSection",
    "value": "15",
    "slotSetting": false
  },
  {
    "name": "Unstructured:ApiKey",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "Unstructured:ApiUrl",
    "value": "https://api.unstructuredapp.io/general/v0/general",
    "slotSetting": false
  },
  {
    "name": "Unstructured:Strategy",
    "value": "fast",
    "slotSetting": false
  },  
  //this is the base url for the document processing service 
  {
    "name": "VectorSearchBaseUrl",
    "value": "https://serverlessragdemo.azurewebsites.net",
    "slotSetting": false
  },
  //this is the base url for the python vector database
  {
    "name": "VectorServiceBaseUrl",
    "value": "https://serverlesslancedbdemo.azurewebsites.net",
    "slotSetting": false
  },
  {
    "name": "VoyageAPIKey",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "VoyageEmbeddingModel",
    "value": "voyage-3-large",
    "slotSetting": false
  },
  //this is the connection string to the blob storage account where the pdfs are stored (lancedb storage account)
  {  
    "name": "BlobStorageConnString",
    "value": "",
    "slotSetting": false
  },

  //serverlesslancedb
  {
    "name": "LanceDbContainerFolderURI",
    "value": "az://lancedb/database",
    "slotSetting": false
  },
  {//account key for the lancedb storage account
    "name": "StorageAccountKey",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "StorageAccountName",
    "value": "serverlesslancedbdemo",
    "slotSetting": false
  }
