using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SonglistSettingsUpdateRequest
{
    [JsonPropertyName("designSettingsJson")]
    public string DesignSettingsJson { get; set; } = "{}";

    [JsonPropertyName("songRequestCommands")]
    public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
    
    [JsonPropertyName("omakases")]
    public List<OmakaseDto> Omakases { get; set; } = new();
}

public class SonglistSettingsResponseDto
{
    [JsonPropertyName("overlayToken")]
    public string? OverlayToken { get; set; }

    [JsonPropertyName("designSettingsJson")]
    public string DesignSettingsJson { get; set; } = "{}";

    [JsonPropertyName("songRequestCommands")]
    public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();

    [JsonPropertyName("omakases")]
    public List<OmakaseDto> Omakases { get; set; } = new();
}

public class SongRequestCommandDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "노래 신청";

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = "!신청";
    
    [JsonPropertyName("price")]
    public int Price { get; set; } = 0;
}

public class OmakaseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "오마카세";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "🍣";

    [JsonPropertyName("price")]
    public int Price { get; set; } = 1000;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";
}
