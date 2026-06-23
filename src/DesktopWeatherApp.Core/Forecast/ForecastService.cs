using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.OpenMeteo;
using DesktopWeatherApp.Core.Weather;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace DesktopWeatherApp.Core.Forecast;

public sealed class ForecastService : IForecastService
{
    public const string HttpClientName = "open-meteo";

    private static readonly LocalDateTimePattern TimePattern =
        LocalDateTimePattern.CreateWithInvariantCulture("uuuu-MM-dd'T'HH:mm");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ForecastService> _logger;

    public ForecastService(IHttpClientFactory httpClientFactory, ILogger<ForecastService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ForecastResult> GetCurrentAsync(Location location, CancellationToken cancellationToken)
    {
        var lat = location.Coordinate.Latitude.ToString(CultureInfo.InvariantCulture);
        var lon = location.Coordinate.Longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"/v1/forecast?latitude={lat}&longitude={lon}" +
                  "&current=temperature_2m,weather_code&timezone=auto";

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogInformation(ex, "Open-Meteo call {Url} failed (network) in {ElapsedMs}ms",
                url, stopwatch.ElapsedMilliseconds);
            return ForecastResult.Failure(ForecastError.NetworkUnavailable);
        }

        _logger.LogInformation("Open-Meteo call {Url} -> {Status} in {ElapsedMs}ms",
            url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
            return ForecastResult.Failure(ForecastError.BadResponse);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse(body);
    }

    private static ForecastResult Parse(string body)
    {
        OpenMeteoCurrentResponse? dto;
        try
        {
            dto = JsonSerializer.Deserialize<OpenMeteoCurrentResponse>(body);
        }
        catch (JsonException)
        {
            return ForecastResult.Failure(ForecastError.BadResponse);
        }

        if (dto?.Current is null ||
            dto.Timezone is null ||
            dto.Current.Time is null ||
            dto.Current.Temperature2m is null ||
            dto.Current.WeatherCode is null)
        {
            return ForecastResult.Failure(ForecastError.BadResponse);
        }

        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(dto.Timezone);
        var parsedTime = TimePattern.Parse(dto.Current.Time);
        if (zone is null || !parsedTime.Success)
            return ForecastResult.Failure(ForecastError.BadResponse);

        var observedAt = parsedTime.Value.InZoneLeniently(zone);
        var conditions = new CurrentConditions(
            dto.Current.Temperature2m.Value,
            WeatherCodeMapper.Map(dto.Current.WeatherCode.Value),
            observedAt);

        return ForecastResult.Success(conditions);
    }
}
