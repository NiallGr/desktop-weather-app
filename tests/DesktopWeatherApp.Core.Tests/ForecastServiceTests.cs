using System.Net;
using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.Forecast;
using DesktopWeatherApp.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DesktopWeatherApp.Core.Tests;

public class ForecastServiceTests
{
    private static readonly Location London =
        new(new Coordinate(51.5074, -0.1278), "London");

    private static ForecastService ServiceReturning(HttpMessageHandler handler) =>
        new(new FakeHttpClientFactory(handler), NullLogger<ForecastService>.Instance);

    [Fact]
    public async Task Maps_a_recorded_response_to_typed_CurrentConditions()
    {
        var json = await File.ReadAllTextAsync("Fixtures/london-current.json");
        var service = ServiceReturning(new StubHttpMessageHandler(HttpStatusCode.OK, json));

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Conditions!.TemperatureCelsius.Should().Be(26.7);
        result.Conditions.Condition.Should().Be(WeatherCondition.Clear); // weather_code 0
        result.Conditions.ObservedAt.Zone.Id.Should().Be("Europe/London");
        result.Conditions.ObservedAt.Hour.Should().Be(17);
        result.Conditions.ObservedAt.Minute.Should().Be(30);
    }

    [Fact]
    public async Task Builds_the_expected_request_url()
    {
        var json = await File.ReadAllTextAsync("Fixtures/london-current.json");
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        var service = ServiceReturning(handler);

        await service.GetCurrentAsync(London, CancellationToken.None);

        handler.LastRequestUri!.AbsoluteUri.Should().Contain("/v1/forecast");
        handler.LastRequestUri.AbsoluteUri.Should().Contain("latitude=51.5074");
        handler.LastRequestUri.AbsoluteUri.Should().Contain("longitude=-0.1278");
        handler.LastRequestUri.AbsoluteUri.Should().Contain("current=temperature_2m,weather_code");
        handler.LastRequestUri.AbsoluteUri.Should().Contain("timezone=auto");
    }
}
