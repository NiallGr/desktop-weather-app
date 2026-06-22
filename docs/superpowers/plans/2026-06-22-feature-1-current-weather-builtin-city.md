# Feature 1 — Current Weather for One Built-in City Implementation Plan

> **For agentic workers:** Do NOT implement this plan directly. It must first pass `/feature-doc-gauntlet` in a clean session, then be broken into stories by `/enate-to-stories`; AFK implementation happens per-story from there. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** On launch, display live Current Conditions (temperature, condition, observation time) for one hard-coded Supported City (London), fetched from Open-Meteo through a typed `ForecastService` and rendered in an Avalonia window.

**Architecture:** A no-Avalonia `Core` class library owns the domain types and `ForecastService` (the only path to Open-Meteo, via `IHttpClientFactory` + `System.Text.Json` + NodaTime). The Avalonia app references `Core`, hosts `MainViewModel` (CommunityToolkit.Mvvm) and the composition root (DI + Serilog + the named HTTP client + the hard-coded London `Location`), and binds three async states. A Tier-1 xUnit project replays a recorded Open-Meteo fixture against a fake `IHttpClientFactory` to prove the seam.

**Tech Stack:** .NET 8 · Avalonia 11 · CommunityToolkit.Mvvm · `Microsoft.Extensions.Http` (`IHttpClientFactory`) · `Microsoft.Extensions.DependencyInjection` · `System.Text.Json` · NodaTime · Serilog (file + Debug sinks, `CompactJsonFormatter`) · xUnit · FluentAssertions.

**Context references:**
- Spec: `docs/superpowers/specs/2026-06-22-feature-1-current-weather-builtin-city-design.md`
- `business-domain-context.md` (this project's `Context.MD` — domain glossary)
- `Technical-Context.MD` (Overriding Principles that apply: #1 keys in OS-secure storage — trivially satisfied, keyless; #2 typed view↔service boundary; #3 no network calls from the view layer)
- ADRs: `docs/adr/0001-curated-city-list-not-search.md`

> An AFK Developer Agent picking up this plan MUST load every file in the Context references block before writing code.

> **Pre-implementation blocker (from the spec):** `api.open-meteo.com` must be added to the environment's network egress allowlist before this plan runs — it is required to ground the Open-Meteo seam (Seam 1 authority, currently *pending*) and to capture the real happy-path JSON fixture in Task 4. Confirm the recorded fixture matches a live response before relying on the Tier-1 suite.

---

## File structure

```
DesktopWeatherApp.sln
src/DesktopWeatherApp.Core/                 (classlib, net8.0, NO Avalonia)
  DesktopWeatherApp.Core.csproj
  Domain/Coordinate.cs                       Coordinate (lat/lon)
  Domain/Location.cs                         Location (Coordinate + DisplayName)
  Domain/WeatherCondition.cs                 enum
  Domain/CurrentConditions.cs                temperature + condition + ObservedAt
  Domain/ForecastError.cs                    enum (NetworkUnavailable, BadResponse)
  Domain/ForecastResult.cs                   Success | Failure discriminated result
  Weather/WeatherCodeMapper.cs               int weather_code -> WeatherCondition
  OpenMeteo/OpenMeteoCurrentResponse.cs      internal System.Text.Json DTOs
  Forecast/IForecastService.cs               typed contract
  Forecast/ForecastService.cs                HTTP + JSON + NodaTime + logging
src/DesktopWeatherApp/                       (Avalonia app, net8.0)
  DesktopWeatherApp.csproj
  Program.cs                                 Serilog bootstrap + DI + Avalonia start
  App.axaml / App.axaml.cs                   resolves MainWindow from DI
  Locations.cs                               hard-coded London Location (composition root)
  ViewModels/MainViewModel.cs                IsLoading / Error / HasData / Current
  Views/MainWindow.axaml / .axaml.cs         three-state panel
tests/DesktopWeatherApp.Core.Tests/          (xUnit)
  DesktopWeatherApp.Core.Tests.csproj
  Fakes/StubHttpMessageHandler.cs            replays a canned response or throws
  Fakes/FakeHttpClientFactory.cs             IHttpClientFactory over the stub handler
  Fixtures/london-current.json               recorded Open-Meteo response (captured live)
  WeatherCodeMapperTests.cs
  ForecastServiceTests.cs
```

---

## Task 1: Scaffold the solution and three projects

**Files:**
- Create: `DesktopWeatherApp.sln`, the three `.csproj` files, project references, package references.

- [ ] **Step 1: Install the Avalonia templates (idempotent)**

Run:
```bash
dotnet new install Avalonia.Templates
```
Expected: "Success" or "already installed".

- [ ] **Step 2: Create the solution and projects**

Run:
```bash
cd /home/user/desktop-weather-app
dotnet new sln -n DesktopWeatherApp
dotnet new classlib -o src/DesktopWeatherApp.Core -f net8.0
rm src/DesktopWeatherApp.Core/Class1.cs
dotnet new avalonia.app -o src/DesktopWeatherApp -f net8.0
dotnet new xunit -o tests/DesktopWeatherApp.Core.Tests -f net8.0
dotnet sln add src/DesktopWeatherApp.Core src/DesktopWeatherApp tests/DesktopWeatherApp.Core.Tests
```
Expected: projects created and added to the solution.

- [ ] **Step 3: Wire project references**

Run:
```bash
dotnet add src/DesktopWeatherApp reference src/DesktopWeatherApp.Core
dotnet add tests/DesktopWeatherApp.Core.Tests reference src/DesktopWeatherApp.Core
```
Expected: references added. (Note: the app references Core; Core references nothing of ours — enforces Principle #3 at the assembly level.)

- [ ] **Step 4: Add package references**

Run:
```bash
# Core: HTTP factory, logging abstraction, NodaTime. NO Avalonia.
dotnet add src/DesktopWeatherApp.Core package Microsoft.Extensions.Http
dotnet add src/DesktopWeatherApp.Core package Microsoft.Extensions.Logging.Abstractions
dotnet add src/DesktopWeatherApp.Core package NodaTime

# App: MVVM, DI, HTTP, Serilog, NodaTime (for formatting)
dotnet add src/DesktopWeatherApp package CommunityToolkit.Mvvm
dotnet add src/DesktopWeatherApp package Microsoft.Extensions.DependencyInjection
dotnet add src/DesktopWeatherApp package Microsoft.Extensions.Http
dotnet add src/DesktopWeatherApp package Serilog
dotnet add src/DesktopWeatherApp package Serilog.Extensions.Logging
dotnet add src/DesktopWeatherApp package Serilog.Sinks.File
dotnet add src/DesktopWeatherApp package Serilog.Sinks.Debug
dotnet add src/DesktopWeatherApp package Serilog.Formatting.Compact
dotnet add src/DesktopWeatherApp package NodaTime

# Tests: assertions + HTTP abstraction for the fake factory
dotnet add tests/DesktopWeatherApp.Core.Tests package FluentAssertions
dotnet add tests/DesktopWeatherApp.Core.Tests package Microsoft.Extensions.Http
```
Expected: packages restored.

- [ ] **Step 5: Build the empty solution**

Run:
```bash
dotnet build
```
Expected: build succeeds (0 errors).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: scaffold solution (Core + Avalonia app + Core.Tests)"
```

---

## Task 2: WeatherCondition enum + WeatherCodeMapper

Maps Open-Meteo's numeric WMO `weather_code` to the domain `WeatherCondition`. Pure function, no IO — the first red-green loop. Unknown codes map to `Unknown`, never throw.

**Files:**
- Create: `src/DesktopWeatherApp.Core/Domain/WeatherCondition.cs`
- Create: `src/DesktopWeatherApp.Core/Weather/WeatherCodeMapper.cs`
- Test: `tests/DesktopWeatherApp.Core.Tests/WeatherCodeMapperTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/DesktopWeatherApp.Core.Tests/WeatherCodeMapperTests.cs`:
```csharp
using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.Weather;
using FluentAssertions;
using Xunit;

namespace DesktopWeatherApp.Core.Tests;

public class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, WeatherCondition.Clear)]
    [InlineData(1, WeatherCondition.MainlyClear)]
    [InlineData(3, WeatherCondition.Cloudy)]
    [InlineData(45, WeatherCondition.Fog)]
    [InlineData(61, WeatherCondition.Rain)]
    [InlineData(71, WeatherCondition.Snow)]
    [InlineData(95, WeatherCondition.Thunderstorm)]
    public void Maps_known_wmo_codes_to_conditions(int code, WeatherCondition expected)
    {
        WeatherCodeMapper.Map(code).Should().Be(expected);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1000)]
    [InlineData(-1)]
    public void Maps_unknown_codes_to_Unknown(int code)
    {
        WeatherCodeMapper.Map(code).Should().Be(WeatherCondition.Unknown);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter WeatherCodeMapperTests`
Expected: FAIL — `WeatherCondition` / `WeatherCodeMapper` do not exist (compile error).

- [ ] **Step 3: Implement WeatherCondition**

Create `src/DesktopWeatherApp.Core/Domain/WeatherCondition.cs`:
```csharp
namespace DesktopWeatherApp.Core.Domain;

public enum WeatherCondition
{
    Unknown = 0,
    Clear,
    MainlyClear,
    Cloudy,
    Fog,
    Rain,
    Snow,
    Thunderstorm
}
```

- [ ] **Step 4: Implement WeatherCodeMapper**

Create `src/DesktopWeatherApp.Core/Weather/WeatherCodeMapper.cs`:
```csharp
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
```

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter WeatherCodeMapperTests`
Expected: PASS (10 tests).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(core): map WMO weather codes to WeatherCondition"
```

---

## Task 3: Domain value types — Coordinate, Location, CurrentConditions, ForecastResult

The typed payloads the seam produces. `ForecastResult` is the discriminated success-or-failure the view-model pattern-matches on (Principle #2 — typed boundary, no exceptions for expected failures).

**Files:**
- Create: `src/DesktopWeatherApp.Core/Domain/Coordinate.cs`, `Location.cs`, `CurrentConditions.cs`, `ForecastError.cs`, `ForecastResult.cs`
- Test: `tests/DesktopWeatherApp.Core.Tests/ForecastResultTests.cs`

- [ ] **Step 1: Write the failing test for ForecastResult**

Create `tests/DesktopWeatherApp.Core.Tests/ForecastResultTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter ForecastResultTests`
Expected: FAIL — types do not exist (compile error).

- [ ] **Step 3: Implement the value types**

Create `src/DesktopWeatherApp.Core/Domain/Coordinate.cs`:
```csharp
namespace DesktopWeatherApp.Core.Domain;

public readonly record struct Coordinate(double Latitude, double Longitude);
```

Create `src/DesktopWeatherApp.Core/Domain/Location.cs`:
```csharp
namespace DesktopWeatherApp.Core.Domain;

public sealed record Location(Coordinate Coordinate, string DisplayName);
```

Create `src/DesktopWeatherApp.Core/Domain/CurrentConditions.cs`:
```csharp
using NodaTime;

namespace DesktopWeatherApp.Core.Domain;

/// <summary>The Current-Conditions facet of a Forecast (widened to add Hourly/Daily in Feature 3).</summary>
public sealed record CurrentConditions(
    double TemperatureCelsius,
    WeatherCondition Condition,
    ZonedDateTime ObservedAt);
```

Create `src/DesktopWeatherApp.Core/Domain/ForecastError.cs`:
```csharp
namespace DesktopWeatherApp.Core.Domain;

public enum ForecastError
{
    NetworkUnavailable,
    BadResponse
}
```

Create `src/DesktopWeatherApp.Core/Domain/ForecastResult.cs`:
```csharp
namespace DesktopWeatherApp.Core.Domain;

/// <summary>Success-or-failure result of a Refresh. Never carries both a value and an error.</summary>
public sealed class ForecastResult
{
    private ForecastResult(bool isSuccess, CurrentConditions? conditions, ForecastError? error)
    {
        IsSuccess = isSuccess;
        Conditions = conditions;
        Error = error;
    }

    public bool IsSuccess { get; }
    public CurrentConditions? Conditions { get; }
    public ForecastError? Error { get; }

    public static ForecastResult Success(CurrentConditions conditions) =>
        new(true, conditions, null);

    public static ForecastResult Failure(ForecastError error) =>
        new(false, null, error);
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter ForecastResultTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(core): add domain value types and ForecastResult"
```

---

## Task 4: ForecastService happy path — the Open-Meteo seam (Seam 1)

Covers **Seam 1 (Open-Meteo current-weather fetch, network-protocol, external)**. Its (c) contract, verbatim from the spec's Seam inventory:

> `GET https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto` over HTTPS, **no authentication**. Success: HTTP 200 `application/json` with top-level `timezone` (string, IANA zone, non-null) and a `current` object (non-null) holding `time` (string, ISO-8601 local, no offset, non-null), `temperature_2m` (number, °C, non-null), `weather_code` (integer WMO code, non-null). On success `CurrentConditions` is fully populated, no null fields; an unknown `weather_code` maps to `Unknown`, not a failure.

The (d) proof is this Task's boundary-crossing test: a fixture **captured from the live Open-Meteo API** replayed through a fake `IHttpClientFactory`, with `System.Text.Json` parse + NodaTime mapping running for real.

**Files:**
- Create: `tests/.../Fakes/StubHttpMessageHandler.cs`, `Fakes/FakeHttpClientFactory.cs`, `Fixtures/london-current.json`
- Create: `src/DesktopWeatherApp.Core/OpenMeteo/OpenMeteoCurrentResponse.cs`, `Forecast/IForecastService.cs`, `Forecast/ForecastService.cs`
- Test: `tests/DesktopWeatherApp.Core.Tests/ForecastServiceTests.cs`

- [ ] **Step 1: Add the recorded fixture**

Create `tests/DesktopWeatherApp.Core.Tests/Fixtures/london-current.json`. This is a representative Open-Meteo response; **it MUST be replaced with / verified against a real response captured from `api.open-meteo.com` once the host is allowlisted** (Seam 1 authority):
```json
{
  "latitude": 51.5,
  "longitude": -0.12,
  "timezone": "Europe/London",
  "current_units": { "time": "iso8601", "temperature_2m": "°C", "weather_code": "wmo code" },
  "current": { "time": "2026-06-22T15:00", "interval": 900, "temperature_2m": 14.3, "weather_code": 3 }
}
```

In `tests/DesktopWeatherApp.Core.Tests/DesktopWeatherApp.Core.Tests.csproj`, ensure fixtures copy to output by adding inside a `<ItemGroup>`:
```xml
<None Update="Fixtures\london-current.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

- [ ] **Step 2: Add the fake HTTP plumbing**

Create `tests/DesktopWeatherApp.Core.Tests/Fakes/StubHttpMessageHandler.cs`:
```csharp
using System.Net;

namespace DesktopWeatherApp.Core.Tests.Fakes;

/// <summary>Returns a canned response, or throws a supplied exception, for any request.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(HttpStatusCode status, string body)
        : this(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        })
    { }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(_responder(request));
    }
}
```

Create `tests/DesktopWeatherApp.Core.Tests/Fakes/FakeHttpClientFactory.cs`:
```csharp
namespace DesktopWeatherApp.Core.Tests.Fakes;

public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;
    public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) =>
        new(_handler, disposeHandler: false) { BaseAddress = new Uri("https://api.open-meteo.com") };
}
```

- [ ] **Step 3: Write the failing happy-path test**

Create `tests/DesktopWeatherApp.Core.Tests/ForecastServiceTests.cs`:
```csharp
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
        result.Conditions!.TemperatureCelsius.Should().Be(14.3);
        result.Conditions.Condition.Should().Be(WeatherCondition.Cloudy); // weather_code 3
        result.Conditions.ObservedAt.Zone.Id.Should().Be("Europe/London");
        result.Conditions.ObservedAt.Hour.Should().Be(15);
        result.Conditions.ObservedAt.Minute.Should().Be(0);
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
```

- [ ] **Step 4: Run to verify it fails**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter ForecastServiceTests`
Expected: FAIL — `ForecastService` / `IForecastService` do not exist (compile error).

- [ ] **Step 5: Implement the Open-Meteo DTOs**

Create `src/DesktopWeatherApp.Core/OpenMeteo/OpenMeteoCurrentResponse.cs`:
```csharp
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
```

- [ ] **Step 6: Implement IForecastService**

Create `src/DesktopWeatherApp.Core/Forecast/IForecastService.cs`:
```csharp
using DesktopWeatherApp.Core.Domain;

namespace DesktopWeatherApp.Core.Forecast;

/// <summary>The typed view↔service boundary. The only path to Open-Meteo (Overriding Principles #2, #3).</summary>
public interface IForecastService
{
    Task<ForecastResult> GetCurrentAsync(Location location, CancellationToken cancellationToken);
}
```

- [ ] **Step 7: Implement ForecastService (happy path + URL)**

Create `src/DesktopWeatherApp.Core/Forecast/ForecastService.cs`:
```csharp
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
```

- [ ] **Step 8: Run to verify it passes**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter ForecastServiceTests`
Expected: PASS (2 tests).

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(core): ForecastService maps Open-Meteo current weather (Seam 1 happy path)"
```

---

## Task 5: ForecastService failure mapping (Seam 1 nullability + error paths)

Completes the (d) proof for Seam 1's failure/nullability handling: transport failure → `NetworkUnavailable`; non-200, non-JSON, and missing-field bodies → `BadResponse`.

**Files:**
- Modify: `tests/DesktopWeatherApp.Core.Tests/ForecastServiceTests.cs` (add cases)

- [ ] **Step 1: Add the failing failure-path tests**

Append these methods inside the `ForecastServiceTests` class in `tests/DesktopWeatherApp.Core.Tests/ForecastServiceTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run to verify the new tests pass**

Run: `dotnet test tests/DesktopWeatherApp.Core.Tests --filter ForecastServiceTests`
Expected: PASS (7 tests total — the 2 from Task 4 plus these 5). The implementation from Task 4 already handles these paths; if any fails, fix `ForecastService.Parse`/error handling until green.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test(core): cover ForecastService failure and nullability paths (Seam 1)"
```

---

## Task 6: MainViewModel (three async states)

App-layer glue. Per the spec, view-model/headless-UI tests are out of scope for Feature 1 (the Core service is the mandated test target), so this Task implements without a dedicated test. State logic is deliberately tiny.

**Files:**
- Create: `src/DesktopWeatherApp/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Implement MainViewModel**

Create `src/DesktopWeatherApp/ViewModels/MainViewModel.cs`:
```csharp
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.Forecast;

namespace DesktopWeatherApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IForecastService _forecastService;
    private readonly Location _location;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _summary;

    public MainViewModel(IForecastService forecastService, Location location)
    {
        _forecastService = forecastService;
        _location = location;
    }

    /// <summary>Fired once when the window opens. No user-triggered re-entry in Feature 1.</summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            var result = await _forecastService.GetCurrentAsync(_location, CancellationToken.None);
            if (result.IsSuccess)
            {
                var c = result.Conditions!;
                Summary = $"{_location.DisplayName} · {c.TemperatureCelsius:0.#}°C · " +
                          $"{c.Condition} · as of {c.ObservedAt:HH:mm}";
                HasData = true;
            }
            else
            {
                Error = "couldn't reach open-meteo.";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/DesktopWeatherApp`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat(app): MainViewModel with IsLoading/Error/HasData states"
```

---

## Task 7: Avalonia UI + composition root (DI, Serilog, HTTP client, London)

Wires everything: Serilog per the Technical-Context instrumentation contract, DI with the named `IHttpClientFactory` client, the hard-coded London `Location`, and the three-state view.

**Files:**
- Create: `src/DesktopWeatherApp/Locations.cs`
- Modify: `src/DesktopWeatherApp/Program.cs`
- Modify: `src/DesktopWeatherApp/App.axaml.cs`
- Create/Modify: `src/DesktopWeatherApp/Views/MainWindow.axaml` and `.axaml.cs`

- [ ] **Step 1: Add the hard-coded London location (composition root)**

Create `src/DesktopWeatherApp/Locations.cs`:
```csharp
using DesktopWeatherApp.Core.Domain;

namespace DesktopWeatherApp;

/// <summary>The single hard-coded Supported City for Feature 1. Core has no notion of a default.</summary>
public static class Locations
{
    public static readonly Location London =
        new(new Coordinate(51.5074, -0.1278), "London");
}
```

- [ ] **Step 2: Replace Program.cs with Serilog + DI bootstrap**

Replace the contents of `src/DesktopWeatherApp/Program.cs`:
```csharp
using System;
using Avalonia;
using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.Forecast;
using DesktopWeatherApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace DesktopWeatherApp;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = default!;

    [STAThread]
    public static void Main(string[] args)
    {
        var logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopWeatherApp", "logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "DesktopWeatherApp")
            .WriteTo.File(new CompactJsonFormatter(),
                System.IO.Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .WriteTo.Debug()
            .CreateLogger();

        try
        {
            Services = BuildServices();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSerilog(dispose: false));
        services.AddHttpClient(ForecastService.HttpClientName,
            c => c.BaseAddress = new Uri("https://api.open-meteo.com"));
        services.AddSingleton<IForecastService, ForecastService>();
        services.AddSingleton(Locations.London);
        services.AddTransient<MainViewModel>();
        return services.BuildServiceProvider();
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
```

> Note: `ForecastService` takes `IHttpClientFactory` + `ILogger<ForecastService>`, both provided by `AddHttpClient` + `AddLogging`. `Location` is registered as a singleton so `MainViewModel` resolves it by type.

- [ ] **Step 3: Resolve MainWindow from DI in App.axaml.cs**

Replace the contents of `src/DesktopWeatherApp/App.axaml.cs`:
```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DesktopWeatherApp.ViewModels;
using DesktopWeatherApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopWeatherApp;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Program.Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

> If `App.axaml` was generated with a different namespace/`x:Class`, keep its existing `x:Class` and adjust the namespace here to match. Do not change `App.axaml` itself beyond what the template generated.

- [ ] **Step 4: Implement the three-state MainWindow view**

Create/replace `src/DesktopWeatherApp/Views/MainWindow.axaml`:
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="DesktopWeatherApp.Views.MainWindow"
        Width="360" Height="180" Title="Weather">
  <Panel Margin="24">
    <TextBlock Text="loading…"
               IsVisible="{Binding IsLoading}"
               VerticalAlignment="Center" HorizontalAlignment="Center"/>
    <TextBlock Text="{Binding Summary}"
               IsVisible="{Binding HasData}"
               FontSize="18" VerticalAlignment="Center" HorizontalAlignment="Center"/>
    <TextBlock Text="{Binding Error}"
               IsVisible="{Binding Error, Converter={x:Static ObjectConverters.IsNotNull}}"
               Foreground="#B00020" VerticalAlignment="Center" HorizontalAlignment="Center"/>
  </Panel>
</Window>
```

Create/replace `src/DesktopWeatherApp/Views/MainWindow.axaml.cs`:
```csharp
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DesktopWeatherApp.ViewModels;

namespace DesktopWeatherApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Opened += async (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                await vm.InitializeAsync();
        };
    }
}
```

> If the Avalonia template generated `MainWindow` at `src/DesktopWeatherApp/MainWindow.axaml` (project root, namespace `DesktopWeatherApp`), either move it into `Views/` with the namespace `DesktopWeatherApp.Views` or keep the template's location and update the `x:Class`, the `using`/namespace in `App.axaml.cs`, and `MainWindow.axaml.cs` to match. The three states and the `Opened` hook are the load-bearing parts.

- [ ] **Step 5: Build**

Run: `dotnet build`
Expected: build succeeds (0 errors).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(app): Avalonia UI, DI, Serilog, hard-coded London location"
```

---

## Task 8: Verify the full suite, format, and a manual run

**Files:** none (verification only).

- [ ] **Step 1: Run the whole test suite**

Run: `dotnet test`
Expected: PASS — all WeatherCodeMapper, ForecastResult, and ForecastService tests green (17 tests total).

- [ ] **Step 2: Format check**

Run: `dotnet format --verify-no-changes`
Expected: no changes required. If it reports changes, run `dotnet format` and commit them.

- [ ] **Step 3: Manual run (requires `api.open-meteo.com` allowlisted)**

Run: `dotnet run --project src/DesktopWeatherApp`
Expected: window opens, shows "loading…" briefly, then `London · NN°C · <Condition> · as of HH:mm`. With networking blocked, it shows `couldn't reach open-meteo.` instead — both are acceptable observations of the three-state wiring.

- [ ] **Step 4: Final commit (if formatting changed anything)**

```bash
git add -A
git commit -m "style: dotnet format" || echo "nothing to format"
```

---

## Self-review

**1. Spec coverage:**
- Three-project Core/app/tests layout → Task 1. ✓
- Minimal Current-Conditions `Forecast` type → Task 3 (`CurrentConditions`). ✓
- `weather_code`→`WeatherCondition`, unknown→`Unknown` → Task 2 + Task 5 (unknown-code case). ✓
- NodaTime `ObservedAt`, never raw `DateTime` → Task 3 type + Task 4 parsing. ✓
- Typed `ForecastService` contract, result-not-exceptions → Task 3 (`ForecastResult`) + Task 4 (`IForecastService`). ✓
- Request URL (lat/lon/current/timezone, keyless) → Task 4 Step 7 + URL test. ✓
- Failure mapping (NetworkUnavailable / BadResponse) → Task 5. ✓
- Three async states + terminal error, terse tone, skeleton-not-spinner → Task 6 + Task 7. ✓
- DI + named HttpClient + Serilog instrumentation → Task 7. ✓
- Mandated Tier-1 recorded-replay of `ForecastService` → Tasks 4 & 5. ✓
- Out-of-scope items (picker, GPS, hourly/daily, cache, scheduled refresh, retry, copy-diagnostics) → none implemented. ✓

**2. Placeholder scan:** No "TBD"/"handle edge cases"/"write tests for the above" — every code step carries real code. The fixture carries an explicit, actionable instruction (capture/verify live), not a vague placeholder.

**3. Type consistency:** `GetCurrentAsync`, `ForecastResult.Success/Failure`, `IsSuccess`/`Conditions`/`Error`, `CurrentConditions(TemperatureCelsius, Condition, ObservedAt)`, `WeatherCodeMapper.Map`, `ForecastService.HttpClientName` used identically across Tasks 2–7. ✓

**4. Seam coverage:** Seam 1 (Open-Meteo, network-protocol, external) → **Task 4** names its (c) contract verbatim and its Step 3/7 write the (d) boundary-crossing test (fixture captured from the live API replayed through a fake `IHttpClientFactory`, real `System.Text.Json` + NodaTime parse), with Task 5 completing the nullability/failure cases. The seam authority remains *pending* until `api.open-meteo.com` is allowlisted — surfaced in the header and Task 4 Step 1. ✓
