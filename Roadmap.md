# Roadmap

**Product:** Desktop Weather App — a single-user Windows desktop app showing the Forecast for one active Location, sourced from Open-Meteo.
**Last reviewed:** 2026-06-22

## Sequencing

Features are listed in delivery order. Each Feature gets its own `/brainstorming` session, Spec, and Plan. Vocabulary follows `business-domain-context.md`; engineering constraints follow `Technical-Context.MD`.

---

## Feature 1: See current weather for one built-in city 🔫 *tracer bullet*

On launch, the app shows the Current Conditions (temperature and condition) for a single hard-coded Supported City, fetched live from Open-Meteo through `ForecastService` and rendered in the Avalonia window. It exercises every layer end-to-end: view → view-model (`IsLoading`/`Error`/`HasData`) → typed `ForecastService` → `IHttpClientFactory` → Open-Meteo → JSON-to-`Forecast` mapping → back to the UI.

**Out of scope:** no city picker, no GPS, no Hourly/Daily facets, no caching/offline, no scheduled refresh, no manual-refresh button. One fixed city, current conditions, fetched once at startup.

**Dependencies:** None (this is the tracer bullet).

**Why first:** it's the thinnest slice that touches every layer and proves the riskiest seam — the live Open-Meteo call mapped to a typed `Forecast`. It also scaffolds the solution (project structure, DI, Serilog, and the Tier-1 `ForecastService` test harness with a fake `IHttpClientFactory`) that every later Feature builds on.

---

## Feature 2: Pick your city from the curated list

The app launches into the empty state (no active Location, no Forecast) and the user picks a Supported City from the curated ~20–30 city dropdown (the City List). Selecting a city sets the active Location and triggers a manual Refresh, showing that city's Current Conditions. Picking a different city replaces the active Location and re-fetches.

**Out of scope:** no GPS/Current Location, no Hourly/Daily facets, no caching/offline, no scheduled refresh, no explicit manual-refresh button (selection-triggered Refresh is enough here). Still current-conditions-only and online-only.

**Dependencies:** Feature 1 (the `ForecastService` → display pipeline; swaps the fixed city for a `CityCatalog`-driven selection and introduces the first-launch empty state).

---

## Feature 3: See the full forecast — hourly and daily

Expand the display from current-conditions-only to the complete Forecast: add the Hourly Forecast (coming hours) and the Daily Forecast (multi-day highs, lows, conditions) views, all drawn from the same single snapshot `ForecastService` already returns.

**Out of scope:** no GPS, no caching/offline, no scheduled refresh. Still city-picker-driven and online-only.

**Dependencies:** Feature 2 (a selected Location producing a Forecast). Internally widens the `Forecast` type and the Open-Meteo mapping established in Features 1–2.

---

## Feature 4: Keep working offline with the last known forecast

Introduce the `ForecastCache`: every successful Refresh writes that Location's Forecast to disk under `%LOCALAPPDATA%`, one entry per Location. When a Refresh can't reach Open-Meteo, the app shows that Location's Last Known Forecast, stamped with when it was retrieved ("last known forecast from 14:32"), instead of blanking the screen. Switching to a never-fetched Location while offline shows the empty state — never another city's data under the wrong heading. Survives app restarts.

**Out of scope:** no GPS, no scheduled refresh. The cache fills only via user-driven (manual / selection) Refreshes for now.

**Dependencies:** Feature 3 (a complete Forecast worth caching). This is where the `ForecastService` cache-fallback policy and its mandated Tier-1 tests (success-writes-cache / failure-returns-last-known / never-fetched-shows-empty) land.

---

## Feature 5: Stay current automatically

Add the `RefreshScheduler`: a timer that triggers a scheduled Refresh of the active Location's Forecast at a fixed interval, with no user action. The previous Forecast stays visible with a subtle "refreshing…" indicator until the new one lands or fails; a failed scheduled Refresh falls back to the Last Known Forecast exactly as a manual one does. Both trigger kinds (manual / scheduled) are now live and logged per the Technical-Context contract.

**Out of scope:** no GPS; no user-configurable interval (fixed for now — a settings/preferences feature is deferred, like Units); no background running while the app is closed (scheduling only while open).

**Dependencies:** Feature 4 (the cache-fallback path a failed scheduled Refresh relies on) and Feature 2 (an active Location to refresh).

---

## Feature 6: Use my current location

Add the `CurrentLocationProvider`: the user can opt into GPS so the active Location becomes the device's Current Location (an arbitrary coordinate, not a curated entry). The Forecast, caching, and both Refresh kinds all work for a GPS Location just as for a Supported City. When GPS is unavailable or permission is denied, the app gives a clear path back to picking a Supported City — never a dead end. GPS coordinates are rounded to ~0.1° before logging, per the Technical-Context contract.

**Out of scope:** no continuous location tracking / auto-follow as the device moves (resolve once when GPS is chosen or refreshed); no reverse-geocoding the coordinate to a city name beyond what's needed to label it.

**Dependencies:** Feature 4 (per-Location caching, so a GPS coordinate is just another Location) and Feature 2 (the location-selection mechanism and the fallback-to-city path). Deliberately last because GPS is the least reliable input on a Windows desktop — everything else should already work without it.

> ADO Work Item: https://dev.azure.com/Enate/Nialls%20Factory%20DevTest/_workitems/edit/94878
