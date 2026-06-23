using DesktopWeatherApp.Core.Domain;

namespace DesktopWeatherApp.Core.Weather;

/// <summary>Maps Open-Meteo WMO weather codes to the domain WeatherCondition.</summary>
public static class WeatherCodeMapper
{
    public static WeatherCondition Map(int wmoCode) => wmoCode switch
    {
        0 => WeatherCondition.Clear,
        1 or 2 => WeatherCondition.MainlyClear,
        3 => WeatherCondition.Cloudy,
        45 or 48 => WeatherCondition.Fog,
        51 or 53 or 55 or 56 or 57 => WeatherCondition.Rain,   // drizzle
        61 or 63 or 65 or 66 or 67 => WeatherCondition.Rain,   // rain
        80 or 81 or 82 => WeatherCondition.Rain,               // rain showers
        71 or 73 or 75 or 77 => WeatherCondition.Snow,         // snow fall / grains
        85 or 86 => WeatherCondition.Snow,                     // snow showers
        95 or 96 or 99 => WeatherCondition.Thunderstorm,
        _ => WeatherCondition.Unknown
    };
}
