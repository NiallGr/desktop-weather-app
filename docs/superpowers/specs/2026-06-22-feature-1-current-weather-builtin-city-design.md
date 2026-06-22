# Feature 1 — See current weather for one built-in city 🔫 (tracer bullet)

**Context references:**
- `business-domain-context.md` (this project's domain glossary; the renamed `Context.MD`)
- `Technical-Context.MD`
- `PRD.md`
- `Roadmap.md` → Feature: 1 — See current weather for one built-in city (tracer bullet)
- `docs/adr/0001-curated-city-list-not-search.md`

## Summary

On launch, the app shows live **Current Conditions** for one hard-coded **Supported City** (London), fetched from **Open-Meteo** through a typed `ForecastService` and rendered in the Avalonia window. This is the tracer bullet: the thinnest vertical slice that exercises every layer — view → view-model (`IsLoading`/`Error`/`HasData`) → typed service → `IHttpClientFactory` → Open-Meteo → `System.Text.Json` mapping → NodaTime timestamp → back to the UI. It also scaffolds the solution structure, DI, Serilog, and the Tier-1 recorded-replay test harness that every later Feature inherits.

## Scope

**In scope:** one fixed city; a single startup fetch; display of temperature + condition + observation time; the three async states (loading / data / error); the three-project solution layout; the mandated Tier-1 test of `ForecastService`.

**Out of scope (deliberately):** the City List picker (Feature 2), GPS / Current Location (Feature 6), the Hourly and Daily Forecast facets (Feature 3), caching / Last Known Forecast / offline (Feature 4), scheduled Refresh (Feature 5), a manual-refresh or Retry button, and the "Copy diagnostics" affordance. A failed startup fetch is terminal until the app is relaunched.

## Architecture

Three projects in one solution:

- **`src/DesktopWeatherApp.Core`** (class library, **no Avalonia reference**) — domain types + the service. The no-Avalonia rule enforces, at the assembly level, the Technical-Context principles that the view↔service boundary is a typed contract and that the view layer never issues HTTP.
- **`src/DesktopWeatherApp`** (Avalonia 11 + CommunityToolkit.Mvvm) — `MainWindow`, `MainViewModel`, and the composition root (DI, `IHttpClientFactory`, Serilog). The hard-coded London `Location` lives here and is passed into `Core`; `Core` has no notion of a default city.
- **`tests/DesktopWeatherApp.Core.Tests`** (xUnit + FluentAssertions) — the Tier-1 recorded-replay suite.

Dependency direction: `DesktopWeatherApp` → `DesktopWeatherApp.Core`, never the reverse.

## Domain types (in `Core`)

Shapes are illustrative, not final code:

```
Coordinate        { double Latitude; double Longitude }
Location          { Coordinate Coordinate; string DisplayName }
WeatherCondition  enum: Clear, MainlyClear, Cloudy, Fog, Rain, Snow, Thunderstorm, Unknown
CurrentConditions { double TemperatureCelsius; WeatherCondition Condition; ZonedDateTime ObservedAt }
ForecastResult    Success(CurrentConditions) | Failure(ForecastError)
ForecastError     enum: NetworkUnavailable, BadResponse   (+ optional detail string for logging only)
```

- `CurrentConditions` is the **Current-Conditions-only** `Forecast` for now (decision: build minimal, widen in Feature 3 — YAGNI; the Open-Meteo seam is proven just as well by one current reading as by the full bundle).
- `WeatherCondition` is the domain mapping of Open-Meteo's numeric `weather_code`. A small lookup in `Core`. **Unrecognised codes map to `Unknown`, never throw** — so a new WMO code never crashes the app. This mapping is the one piece Feature 3 reuses.
- `ObservedAt` is a NodaTime `ZonedDateTime` (never raw `DateTime`), built from Open-Meteo's local `current.time` plus the response's IANA `timezone`.

## Service contract

`IForecastService.GetCurrentAsync(Location, CancellationToken) → Task<ForecastResult>`

- The single typed contract the view-model calls. Expected failure modes (transport failure, unparseable response) return `ForecastResult.Failure` — they are **not** thrown across the boundary. Genuinely unexpected exceptions bubble and are logged at `Error` (per Technical-Context's `AppDomain`/`TaskScheduler` wiring).
- Owns the `HttpClient` (named `IHttpClientFactory` client), the Open-Meteo request, the JSON→domain mapping, and structured logging of the call (URL sans secrets, status, latency) per the Technical-Context instrumentation contract.

## Data flow (one pass, at startup)

```
MainViewModel (on window open) → IForecastService.GetCurrentAsync(London)
  → HttpClient ("open-meteo" client)
    → GET https://api.open-meteo.com/v1/forecast
         ?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto
  → System.Text.Json deserialize → map to CurrentConditions → ForecastResult.Success
  ↘ transport failure / timeout         → ForecastResult.Failure(NetworkUnavailable)
  ↘ non-200 / non-JSON / missing fields → ForecastResult.Failure(BadResponse)
→ view-model sets HasData or Error; the call is logged at Information
```

## UI & view-model

- **`MainViewModel`** (CommunityToolkit.Mvvm): observable `IsLoading` (bool), `Error` (string?, null when none), `HasData` (bool), `Current` (`CurrentConditions?`). An async initialise fired **once** on window open; no user-triggered re-entry.
  - `IsLoading=true` → `await GetCurrentAsync(London)` → `Success`: set `Current`, `HasData=true`; `Failure`: set `Error` to a terse message; `finally IsLoading=false`.
- **`MainWindow` (XAML)** — one panel, three mutually exclusive states:
  - **Loading:** skeleton placeholder (not a spinner overlay — Technical-Context).
  - **Data:** e.g. `London · 14°C · Partly cloudy · as of 15:20` (`ObservedAt` formatted in its zone).
  - **Error:** terse, factual text, e.g. `Couldn't reach Open-Meteo.` — no Retry button, no stack trace.
- Tone: lowercase-acceptable status text, no emoji, no apology (Technical-Context "User Feedback Approach").

## Error handling

- `NetworkUnavailable` and `BadResponse` both render the Error state; the distinction exists for logging, not for differing UI in F1.
- No cache and no Retry: a failed startup fetch is terminal until relaunch. This is an accepted tracer-bullet rough edge; recovery arrives with Features 2 (manual Refresh) and 4 (Last Known Forecast).

## Testing

`ForecastService` is the mandated Tier-1 target (Technical-Context). Tests assert the deterministic envelope, never Open-Meteo's prose.

- **Harness:** fake `IHttpClientFactory` → `HttpClient` over a stub `HttpMessageHandler` that replays a recorded Open-Meteo JSON fixture checked into the test project. Establishes the recorded-replay pattern later seams reuse (no prior art — this is the first).
- **Cases:**
  1. **Happy path** — fixture with `temperature_2m` + `weather_code` + `time`/`timezone` → `Success` with correct `TemperatureCelsius`, mapped `WeatherCondition`, and `ObservedAt` `ZonedDateTime` in the expected zone.
  2. **weather_code mapping** — a representative code maps correctly; an **unknown code → `Unknown`** (no throw).
  3. **Network failure** — handler throws `HttpRequestException` / times out → `Failure(NetworkUnavailable)`.
  4. **Bad response** — malformed / missing-field JSON → `Failure(BadResponse)`.
- **Real-IO on one side:** `System.Text.Json` parse + NodaTime mapping run for real; only HTTP transport is faked (satisfies "every seam gets a real-IO test on at least one side").
- **Out of scope for F1 tests:** cache-write / Last-Known fallback / empty-state (Feature 4); view-model / Avalonia-headless tests (Core service is the mandated target).
- **Fixture provenance:** the happy-path fixture must be a *real* response captured from the live Open-Meteo API — see the seam authority gap below.

## Pre-implementation tasks

- **Egress allowlist (runtime only):** `api.open-meteo.com` must be added to the environment's network egress allowlist for any environment that will **run** the app (Plan Task 8 Step 3 manual `dotnet run` smoke test, plus end-user use). The Tier-1 test suite does **not** need this — it replays the committed fixture against a fake `IHttpClientFactory`. Seam 1 `(e)` is **grounded** (live capture committed 2026-06-22); no remaining doc-stage blocker.

## Seam inventory

### Seam 1: Open-Meteo current-weather fetch
- **(a) class:** network-protocol (with a data-format/nullability facet) — **external**
- **(b) sides:** `ForecastService` (our code, `Core`) ↔ Open-Meteo `GET /v1/forecast` HTTP endpoint (third-party service)
- **(c) contract:**
  - **Auth (first contact, pinned):** Open-Meteo's public forecast API takes **no authentication** — no API key, header, or token for non-commercial use. No secret crosses this boundary. (Later Open-Meteo seams inherit this by reference. To confirm at grounding.)
  - **Request:** `GET https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto` over HTTPS.
  - **Success payload:** HTTP 200, `application/json`; body has a top-level `timezone` (string, IANA zone name, non-null) and a `current` object (non-null) containing `time` (string, ISO-8601 local, no offset, non-null), `temperature_2m` (number, °C, non-null), and `weather_code` (integer WMO code, non-null).
  - **Shape/nullability handling:** any of {non-200, non-JSON, absent `current`, absent/null any of the three fields, unparseable `time`/`timezone`} → `ForecastResult.Failure(BadResponse)`; transport failure/timeout → `Failure(NetworkUnavailable)`. An **unknown** integer `weather_code` is valid wire data → maps to `WeatherCondition.Unknown` (not a failure). On `Success`, `CurrentConditions` is fully populated with no null fields.
- **(d) proof:** Tier-1 recorded-replay test in `DesktopWeatherApp.Core.Tests` — fake `IHttpClientFactory` replays the JSON fixture **captured live from `https://api.open-meteo.com/v1/forecast?latitude=51.5074&longitude=-0.1278&current=temperature_2m,weather_code&timezone=auto` on 2026-06-22 17:30 Europe/London** (committed verbatim, not hand-edited), asserting the typed `CurrentConditions` and each failure-mapping case. Real-IO side: `System.Text.Json` parse + NodaTime mapping execute for real.
- **(e) authority:** Open-Meteo Forecast API itself — **GROUNDED 2026-06-22:** a live `/v1/forecast` response was captured (see (d) above) and the contract above was verified against it field-by-field: top-level `timezone` is `"Europe/London"` (IANA string); `current.time` is `"2026-06-22T17:30"` (ISO-8601 local, no offset, **no seconds** — confirming the Plan's `LocalDateTimePattern "uuuu-MM-dd'T'HH:mm"` is correct); `current.temperature_2m` is a number (`26.7`); `current.weather_code` is an integer (`0` → Clear). The response carries additional fields (`generationtime_ms`, `utc_offset_seconds`, `timezone_abbreviation`, `elevation`, `current_units.interval`) which our DTO ignores by design. No authentication header was sent or required.

### Seam 2: NodaTime IANA timezone resolution (host-OS / runtime, internal)
- **(a) class:** host-OS / runtime — a data-format/version facet over the bundled timezone database. **Internal** (no network), but a real boundary because the TZDB ships with the NodaTime package version, not with our code.
- **(b) sides:** `ForecastService` mapping code (`Core`) ↔ NodaTime `DateTimeZoneProviders.Tzdb` (the bundled IANA TZDB) — resolving the IANA zone name that Open-Meteo's `timezone=auto` returns into a `DateTimeZone`, then projecting the local `time` into a `ZonedDateTime` (`ObservedAt`).
- **(c) contract:**
  - **Resolver:** the `DateTimeZoneProviders.Tzdb` instance (NodaTime's bundled IANA TZDB), not the host OS zone database — so behaviour is deterministic across machines and pinned to the NodaTime package version, never the host's installed tzdata.
  - **Unknown zone name:** `GetZoneOrNull(name)` returns `null` for any IANA name absent from the bundled TZDB (including a name newer than the bundled snapshot) → mapped to `ForecastResult.Failure(BadResponse)`, **never throws**. The same as any other unparseable-field path.
  - **Ambiguous / skipped local time (DST):** `current.time` is a wall-clock local time with no offset, so a value falling in a DST gap or fold is genuinely ambiguous. Resolution is **lenient** (`InZoneLeniently`) — gaps shift forward, ambiguities pick the earlier offset — yielding a valid `ZonedDateTime` rather than a failure. Accepted for F1: a once-an-hour observation timestamp tolerates the rare ~1h edge case; it is never thrown and never blanks the screen.
- **(d) proof:** exercised by the Seam 1 Tier-1 happy-path test — the recorded fixture's `timezone` (`Europe/London`) resolves against the bundled TZDB and the asserted `ObservedAt.Zone.Id` / `Hour` / `Minute` confirm the real NodaTime resolution + projection ran (NodaTime is the real-IO side, not faked). An explicit unknown-zone-name case (zone string absent from the TZDB → `BadResponse`) SHOULD be added alongside the existing bad-response cases in Task 5.
- **(e) authority:** NodaTime docs — `DateTimeZoneProviders.Tzdb`, `GetZoneOrNull`, and `LocalDateTime.InZoneLeniently`. Library behaviour is grounded in the package itself (no external network), so this seam is not blocked on the egress allowlist; the bundled-TZDB version is pinned by the `NodaTime` package reference in `Core`.

## Feature-doc-gauntlet sign-off

- **Result:** pass
- **Date:** 2026-06-22
- **Summary:** All three leaves pass. Seam 1 (Open-Meteo) is grounded with a verbatim live capture and a verified field-by-field `(c)` contract; Seam 2 (NodaTime TZDB) has falsifiable `(c)` and a real-IO `(d)` covering both happy-path resolution and the unknown-IANA-zone negative path; Spec/Plan introduce no drift from ADR-0001 or the Technical-Context Overriding Principles; no orphaned references or contradictions touching Feature 1.
- **Leaves:** check-seam-cynicism (pass), check-doc-adr-consistency (pass), check-artefact-consistency (pass)
- **Next step:** Feature is cleared for `/enate-to-stories`. _Note: this sign-off becomes stale if the Spec or Plan is materially edited — re-run the gauntlet before story breakdown if either changes._
