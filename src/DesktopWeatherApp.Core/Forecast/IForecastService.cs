using DesktopWeatherApp.Core.Domain;

namespace DesktopWeatherApp.Core.Forecast;

/// <summary>The typed view↔service boundary. The only path to Open-Meteo (Overriding Principles #2, #3).</summary>
public interface IForecastService
{
    Task<ForecastResult> GetCurrentAsync(Location location, CancellationToken cancellationToken);
}
