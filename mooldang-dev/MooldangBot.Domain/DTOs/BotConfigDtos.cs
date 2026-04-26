using System.Text.Json.Serialization;

namespace MooldangBot.Domain.DTOs;

public class BotToggleRequest
{
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }
}

public class SlugUpdateRequest
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}

public class BotConfigRequest
{
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }
    
    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }
    
    [JsonPropertyName("redirectUrl")]
    public string? RedirectUrl { get; set; }
}
