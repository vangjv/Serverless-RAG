using System.Text.Json.Serialization;

namespace ServerlessRAG.Unstructured.Models
{
    public class Link
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("start_index")]
        public int StartIndex { get; set; }
    }
}
