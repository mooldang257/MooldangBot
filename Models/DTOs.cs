using System.Text.Json.Serialization;

namespace MooldangAPI.Models
{
    public class SetupRequest
    {
        public string ChzzkUid { get; set; } = "";
    }

    public class SonglistSettingsUpdateRequest
    {
        public string SongCommand { get; set; } = "!신청";
        public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
        public int SongPrice { get; set; } = 0;
        public string DesignSettingsJson { get; set; } = "{}";
        public List<OmakaseDto> Omakases { get; set; } = new();
    }

    public class SongRequestCommandDto
    {
        public string Keyword { get; set; } = "!신청";
        public int Price { get; set; } = 0;
    }

    public class OmakaseDto
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; } = "오마카세";

        [JsonPropertyName("Command")]
        public string Command { get; set; } = "!물마카세";

        [JsonPropertyName("Icon")]
        public string Icon { get; set; } = "🍣";

        [JsonPropertyName("Price")]
        public int Price { get; set; } = 1000;

        [JsonPropertyName("Count")]
        public int Count { get; set; }
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

    public class PeriodicMessageDto
    {
        public int Id { get; set; }
        public int IntervalMinutes { get; set; }
        public string Message { get; set; } = "";
        public bool IsEnabled { get; set; }
    }

    public class OverlayPresetDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("configJson")]
        public string ConfigJson { get; set; } = "{}";

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class SharedComponentDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("configJson")]
        public string ConfigJson { get; set; } = "{}";
    }

    public class CombinedCommandDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty; // "Custom:12", "Roulette:5" 등

        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "Custom", "Song", "Attendance", "Point", "Roulette", "Omakase"

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("actionType")]
        public string? ActionType { get; set; }

        [JsonPropertyName("requiredRole")]
        public string RequiredRole { get; set; } = "all";
    }
}
