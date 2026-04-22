using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter<RouletteType>))]
    public enum RouletteType
    {
        Cheese,
        ChatPoint
    }
}
