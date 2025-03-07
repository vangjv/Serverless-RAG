using System.Text.Json.Serialization;

namespace ServerlessRAG.Unstructured.Models
{
    public class Metadata
    {
        [JsonPropertyName("filetype")]
        public string Filetype { get; set; }

        [JsonPropertyName("languages")]
        public List<string> Languages { get; set; }

        [JsonPropertyName("page_number")]
        public int PageNumber { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        // Optional properties – present only in some elements
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        [JsonPropertyName("links")]
        public List<Link> Links { get; set; }

        [JsonPropertyName("text_as_html")]
        public string TextAsHtml { get; set; }
    }
}
