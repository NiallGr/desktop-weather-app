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

    [Fact]
    public async Task Returns_NetworkUnavailable_when_transport_throws()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("down"));
        var service = ServiceReturning(handler);

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ForecastError.NetworkUnavailable);
    }

    [Fact]
    public async Task Returns_BadResponse_on_non_success_status()
    {
        var service = ServiceReturning(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "{}"));

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.Error.Should().Be(ForecastError.BadResponse);
    }

    [Fact]
    public async Task Returns_BadResponse_on_unparseable_body()
    {
        var service = ServiceReturning(new StubHttpMessageHandler(HttpStatusCode.OK, "not json"));

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.Error.Should().Be(ForecastError.BadResponse);
    }

    [Fact]
    public async Task Returns_BadResponse_when_current_block_missing()
    {
        var service = ServiceReturning(
            new StubHttpMessageHandler(HttpStatusCode.OK, "{\"timezone\":\"Europe/London\"}"));

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.Error.Should().Be(ForecastError.BadResponse);
    }

    [Fact]
    public async Task Maps_unknown_weather_code_to_Unknown_not_failure()
    {
        const string body =
            "{\"timezone\":\"Europe/London\",\"current\":" +
            "{\"time\":\"2026-06-22T15:00\",\"temperature_2m\":10.0,\"weather_code\":1234}}";
        var service = ServiceReturning(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Conditions!.Condition.Should().Be(WeatherCondition.Unknown);
    }

    // Seam 2 (NodaTime TZDB) negative-path proof: an IANA name absent from the
    // bundled TZDB → GetZoneOrNull returns null → BadResponse, never throws.
    [Fact]
    public async Task Returns_BadResponse_when_timezone_is_not_a_known_IANA_name()
    {
        const string body =
            "{\"timezone\":\"Atlantis/Lost\",\"current\":" +
            "{\"time\":\"2026-06-22T15:00\",\"temperature_2m\":10.0,\"weather_code\":0}}";
        var service = ServiceReturning(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await service.GetCurrentAsync(London, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ForecastError.BadResponse);
    }
}
