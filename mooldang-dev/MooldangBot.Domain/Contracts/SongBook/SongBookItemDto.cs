using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SongBookDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("pitch")]
    public string? Pitch { get; set; }

    [JsonPropertyName("proficiency")]
    public string? Proficiency { get; set; }

    [JsonPropertyName("lyricsUrl")]
    public string? LyricsUrl { get; set; }

    [JsonPropertyName("referenceUrl")]
    public string? ReferenceUrl { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("requiredPoints")]
    public int RequiredPoints { get; set; } = 0;

    [JsonPropertyName("updatedAt")]
    public KstClock? UpdatedAt { get; set; }
}
