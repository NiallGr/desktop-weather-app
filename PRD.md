# PRD — Desktop Weather App

A single-user Windows desktop app that shows the current, hourly, and daily Forecast for one active Location, sourced from Open-Meteo. Vocabulary follows `business-domain-context.md`; engineering constraints follow `Technical-Context.MD`; the curated-city decision is recorded in `docs/adr/0001-curated-city-list-not-search.md`.

## Problem Statement

I want to glance at the weather for a place I care about without opening a browser, hunting through ads, or trusting a site with my data. When my network drops I still want to see the last weather I was shown, clearly marked as old, rather than a blank screen or a spinner that never resolves.

## Solution

A lightweight desktop app that shows the Forecast — Current Conditions, an Hourly Forecast, and a multi-day Daily Forecast — for one active Location at a time. I pick that Location either by choosing a Supported City from a short curated City List, or by letting the app use my Current Location (GPS). The app refreshes on a schedule and on demand. If a Refresh can't reach Open-Meteo, it shows that Location's Last Known Forecast stamped with when it was retrieved; if there's nothing cached for that Location, it shows a clear empty state instead.

## User Stories

1. As a user, I want to open the app and immediately be prompted to choose a Location, so that I understand the app has no Forecast until I pick a place.
2. As a user, I want to pick a Supported City from a curated City List, so that I can see the weather for a major city without typing or searching.
3. As a user, I want the City List capped at a manageable size (~20–30 cities), so that the picker stays simple and uncluttered.
4. As a user, I want to use my Current Location via GPS, so that I can see the weather where I am without picking a city.
5. As a user, I want selecting a new Location to replace the previous one, so that there is never any confusion about which place I'm looking at.
6. As a user, I want to see the Current Conditions (temperature and conditions now) for the active Location, so that I know what it's like right now.
7. As a user, I want to see an Hourly Forecast for the coming hours, so that I can plan the rest of my day.
8. As a user, I want to see a multi-day Daily Forecast with daily highs, lows, and conditions, so that I can plan ahead.
9. As a user, I want all three Forecast facets to come from one coherent snapshot, so that the "now", "hourly", and "daily" views never disagree with each other.
10. As a user, I want the app to refresh the Forecast automatically on a schedule, so that what I see stays current without my intervention.
11. As a user, I want to trigger a manual Refresh on demand, so that I can get the latest Forecast immediately when I want it.
12. As a user, I want selecting a Location to fetch its Forecast straight away, so that I see weather as soon as I choose a place.
13. As a user, when a Refresh fails to reach Open-Meteo, I want to see the active Location's Last Known Forecast, so that I still have useful information when offline.
14. As a user, I want the Last Known Forecast clearly stamped with when it was retrieved (e.g. "last known forecast from 14:32"), so that I know how old it is.
15. As a user, I want a failed Refresh to leave the existing Forecast in place rather than blanking it, so that I never lose what I was already looking at.
16. As a user, when I switch to a Location I've never fetched and I'm offline, I want a clear empty state, so that I'm not shown another city's data under the wrong heading.
17. As a user, I never want to see one city's weather mislabelled as another's, so that I can always trust the heading matches the data.
18. As a user, I want a non-blocking, terse message when Open-Meteo can't be reached, so that I'm informed without being interrupted by alarmist pop-ups.
19. As a user, I want the previous Forecast to stay visible with a subtle "refreshing…" indicator during a Refresh, so that the screen doesn't flash or empty while loading.
20. As a user, I never want to see a raw stack trace in the UI, so that errors stay readable; a "Copy diagnostics" action is enough when I need to report a problem.
21. As a user, I want temperatures and wind shown in a consistent, fixed unit system for now, so that the display is predictable.
22. As a user on Windows, I want the app to run as an installed desktop application, so that I don't depend on a browser or a logged-in website.
23. As a user, I want the app to keep working across restarts by remembering the Last Known Forecast on disk, so that I see something useful immediately after reopening offline.
24. As a user, when GPS is unavailable or permission is denied, I want a clear path to pick a Supported City instead, so that I'm never stuck with no way to see weather.

## Implementation Decisions

- **`ForecastService` is the view↔service contract.** It takes a `Location` and returns a typed `Forecast` (Current Conditions + Hourly Forecast + Daily Forecast). It is the only path to Open-Meteo; no view or view-model issues HTTP directly (Technical-Context Overriding Principle #3). End-to-end typed — no `dynamic`/`object` across the boundary (Principle #2).
- **`ForecastService` owns the cache-fallback policy.** On a successful Refresh it writes the result to `ForecastCache`; on failure it returns that Location's Last Known Forecast (if any), or signals the empty state. The freshness "retrieved-at" stamp travels with the Forecast.
- **`ForecastCache` is a per-Location store.** Keyed by a resolved location key, persisted under `%LOCALAPPDATA%\DesktopWeatherApp`. One Last Known Forecast per Location; a never-fetched Location has none. Hides file IO and serialization.
- **`CurrentLocationProvider` abstracts GPS behind an interface.** Resolves the Current Location to a coordinate; the GPS-unavailable / permission-denied paths return a clear "no location" result rather than throwing. Keeps the rest of the app platform-agnostic.
- **`CityCatalog` supplies the curated City List.** A fixed set of ~20–30 Supported Cities with lookup by key. Manual selection is confined to this catalog — there is no free-text geocoded search (ADR 0001).
- **`RefreshScheduler` triggers scheduled Refreshes on a timer.** Manual Refresh (the refresh action and Location-change) and scheduled Refresh are the only two trigger kinds; there is no separate "load" concept.
- **`MainViewModel` exposes `IsLoading`, `Error`, and `HasData`** (CommunityToolkit.Mvvm) and binds the three Forecast facets, the location picker, the GPS toggle, and the manual-refresh command. The view binds all three async states; empty and error states are first-class.
- **Timestamps use NodaTime** (`Instant` / `ZonedDateTime`) end to end — never raw `DateTime` for Forecast times.
- **Stack: .NET 8 + Avalonia 11 + CommunityToolkit.Mvvm**, DI via `Microsoft.Extensions.DependencyInjection`, HTTP via `IHttpClientFactory`, JSON via `System.Text.Json`, logging via Serilog. Open-Meteo is the sole external dependency (no API key).
- **Units are fixed (metric) for now.** A user-selectable `Units` concept is deferred until a preferences feature is planned (not minted speculatively).

## Testing Decisions

- **What makes a good test here:** assert on observable behaviour and the deterministic envelope — the typed `Forecast` shape, state transitions (`IsLoading`/`Error`/`HasData`), and the cache-fallback side effects — never on internal wiring or on Open-Meteo's exact prose. Test the contract, not the upstream service.
- **`ForecastService` is the mandated test target for this PRD.** Tier-1 recorded-replay: a fake `IHttpClientFactory` replays recorded Open-Meteo JSON fixtures, and tests assert the mapping to the typed `Forecast`, NodaTime timestamp handling, and the three fallback behaviours — fresh success writes the cache, network failure returns the Last Known Forecast, and a never-fetched Location surfaces the empty state. This is the Open-Meteo seam, and per Technical-Context every seam gets a real-IO test on at least one side (here, real local cache IO with a fake HTTP side).
- **The standing ratchet still applies** to the other modules (`ForecastCache`, `CurrentLocationProvider`, `CityCatalog`) under the Technical-Context Tier-1/seam standard, but this PRD does not separately mandate dedicated suites for them — they ride the per-Feature coverage plan and the Tier-3→cheap-tier ratchet.
- **Prior art:** none yet — this is the first substantive module. The `ForecastService` Tier-1 suite establishes the recorded-replay fixture pattern (fake `IHttpClientFactory` + checked-in Open-Meteo JSON) that later seams reuse.

## Out of Scope

- A saved collection of multiple Locations (a `Saved Location` concept) — one active Location only for now.
- Free-text / geocoded location search — manual selection is the curated City List only (ADR 0001).
- User-selectable Units (°C/°F, km/h/mph, 12/24h) — fixed metric for now.
- macOS / Linux builds — Windows-first; cross-platform keyring and runners are a later ADR.
- Weather alerts, radar/maps, historical weather, and notifications.
- Any telemetry, metrics, or traces (Technical-Context: none without an ADR).
- Release/packaging/installer artefacts — local dev only today; a release ADR lands when the first release is planned.

## Further Notes

- Feedback tone is terse and factual, no apologies or emoji (Technical-Context "User Feedback Approach"). Loading uses skeletons / a subtle "refreshing…" indicator, not spinner overlays; the previous Forecast stays visible until the new one lands or fails.
- Logging follows the Technical-Context instrumentation contract: every outbound Open-Meteo call and every Refresh trigger logged at Information with a correlation id; GPS coordinates rounded to ~0.1° before logging; no response bodies, no future API keys.
- This is a multi-feature product; the next step is `/roadmap` to sequence the Features (e.g. forecast retrieval, location selection, caching/offline, scheduling) before per-Feature `/brainstorming`.

> ADO Work Item: https://dev.azure.com/Enate/Nialls%20Factory%20DevTest/_workitems/edit/94878
