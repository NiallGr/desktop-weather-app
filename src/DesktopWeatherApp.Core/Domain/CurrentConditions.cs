using NodaTime;

namespace DesktopWeatherApp.Core.Domain;

/// <summary>The Current-Conditions facet of a Forecast (widened to add Hourly/Daily in Feature 3).</summary>
public sealed record CurrentConditions(
    double TemperatureCelsius,
    WeatherCondition Condition,
    ZonedDateTime ObservedAt);
