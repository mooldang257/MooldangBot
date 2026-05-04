using System.Text.Json.Serialization;

namespace MooldangBot.Domain.DTOs;

public class BotToggleRequest
{
    public bool IsEnabled { get; set; }
}

public class SlugUpdateRequest
{
    public string Slug { get; set; } = string.Empty;
}

public class BotConfigRequest
{
    public string? ClientId { get; set; }
    
    public string? ClientSecret { get; set; }
    
    public string? RedirectUrl { get; set; }
}
