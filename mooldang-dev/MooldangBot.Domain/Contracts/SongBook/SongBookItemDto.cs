using System.Text.Json.Serialization;
using MooldangBot.Domain.Common;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SongBookDto
{
    public int Id { get; set; }
 
    public string Title { get; set; } = string.Empty;
 
    public string? Artist { get; set; }
 
    public string? Category { get; set; }
 
    public string? Pitch { get; set; }
 
    public string? Proficiency { get; set; }
 
    public string? LyricsUrl { get; set; }
 
    public string? ReferenceUrl { get; set; }
 
    public string? ThumbnailUrl { get; set; }
 
    public int RequiredPoints { get; set; } = 0;
 
    public KstClock? UpdatedAt { get; set; }
}
