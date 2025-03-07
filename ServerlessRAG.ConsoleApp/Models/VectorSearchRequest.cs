using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServerlessRAG.ConsoleApp.Models
{

    public class VectorSearchRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("orgId")]
        public string OrgId { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("embeddingModel")]
        public string EmbeddingModel { get; set; }

        [JsonPropertyName("returnVector")]
        public bool ReturnVector { get; set; }

        // Serialize the object to JSON string
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        // Deserialize a JSON string to an object
        public static VectorSearchRequest FromJson(string json)
        {
            return JsonSerializer.Deserialize<VectorSearchRequest>(json);
        }
    }

}
