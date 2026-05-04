using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MooldangBot.Domain.Contracts.SongBook;

public class SonglistSettingsUpdateRequest
{
    public string DesignSettingsJson { get; set; } = "{}";
 
    public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
    
    public List<OmakaseDto> Omakases { get; set; } = new();
}

public class SonglistSettingsResponseDto
{
    public string? OverlayToken { get; set; }
 
    public string DesignSettingsJson { get; set; } = "{}";
 
    public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
 
    public List<OmakaseDto> Omakases { get; set; } = new();
}

public class SongRequestCommandDto
{
    public string Name { get; set; } = "노래 신청";
 
    public string Keyword { get; set; } = "!신청";
    
    public int Price { get; set; } = 0;
}

public class OmakaseDto
{
    public int Id { get; set; }
 
    public string Name { get; set; } = "오마카세";
 
    public string Icon { get; set; } = "🍣";
 
    public int Price { get; set; } = 1000;
 
    public int Count { get; set; }
 
    public string Command { get; set; } = "";
}
