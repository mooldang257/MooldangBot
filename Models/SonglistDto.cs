using System.Text.Json.Serialization;

namespace MooldangAPI.Models
{
    public class OmakaseDto
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Count")]
        public int Count { get; set; }

        [JsonPropertyName("Icon")]
        public string Icon { get; set; } = string.Empty;
    }

    public class SongQueueDto
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("Artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("SortOrder")]
        public int SortOrder { get; set; }
    }

    public class SonglistDataDto
    {
        [JsonPropertyName("Memo")]
        public string Memo { get; set; } = string.Empty;

        [JsonPropertyName("Omakases")]
        public List<OmakaseDto> Omakases { get; set; } = new();

        [JsonPropertyName("Songs")]
        public List<SongQueueDto> Songs { get; set; } = new();
    }
}
