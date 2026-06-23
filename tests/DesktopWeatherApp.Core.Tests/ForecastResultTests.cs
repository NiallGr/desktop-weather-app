using DesktopWeatherApp.Core.Domain;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace DesktopWeatherApp.Core.Tests;

public class ForecastResultTests
{
    [Fact]
    public void Success_carries_conditions_and_no_error()
    {
        var when = new ZonedDateTime(Instant.FromUtc(2026, 6, 22, 14, 0), DateTimeZone.Utc);
        var conditions = new CurrentConditions(14.3, WeatherCondition.Cloudy, when);

        var result = ForecastResult.Success(conditions);

        result.IsSuccess.Should().BeTrue();
        result.Conditions.Should().Be(conditions);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_carries_error_and_no_conditions()
    {
        var result = ForecastResult.Failure(ForecastError.NetworkUnavailable);

        result.IsSuccess.Should().BeFalse();
        result.Conditions.Should().BeNull();
        result.Error.Should().Be(ForecastError.NetworkUnavailable);
    }
}
