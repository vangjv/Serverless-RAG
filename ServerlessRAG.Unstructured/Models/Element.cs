using System.Text.Json.Serialization;

namespace ServerlessRAG.Unstructured.Models
{
    public class Element
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("element_id")]
        public string ElementId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }
    }

}
