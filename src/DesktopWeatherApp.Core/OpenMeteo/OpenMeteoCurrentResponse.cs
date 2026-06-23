using System.Text.Json.Serialization;

namespace DesktopWeatherApp.Core.OpenMeteo;

/// <summary>Wire shape of the Open-Meteo /v1/forecast response (current block only).</summary>
internal sealed class OpenMeteoCurrentResponse
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("current")]
    public CurrentBlock? Current { get; init; }

    internal sealed class CurrentBlock
    {
        [JsonPropertyName("time")]
        public string? Time { get; init; }

        [JsonPropertyName("temperature_2m")]
        public double? Temperature2m { get; init; }

        [JsonPropertyName("weather_code")]
        public int? WeatherCode { get; init; }
    }
}
