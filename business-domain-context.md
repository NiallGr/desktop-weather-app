# Domain Glossary

The shared vocabulary of the Desktop Weather App. Definitions only — no implementation, no process. Related terms are linked by reuse so the language forms one web.

## Current Conditions
The present-moment facet of a Forecast: temperature, conditions, and related now-values for a Location. A facet of a Forecast, not a separate fetch — distinct from the Hourly Forecast (near-future) and Daily Forecast (multi-day).

## Current Location
The Location derived from the device's GPS position, as opposed to one the user enters by hand. One of the two ways of setting the active Location; the other is manual entry. Not a saved place and not a separate place from the active Location — when GPS is the source, the Current Location *is* the active Location.

## Daily Forecast
The multi-day facet of a Forecast: one summary entry per day (typically high/low and conditions) over the forecast horizon. Distinct from the Hourly Forecast (finer-grained, near-term) and Current Conditions (now).

## Forecast
The complete weather picture for a single Location at a point in time, bundling Current Conditions, the Hourly Forecast, and the Daily Forecast. One Forecast is one coherent snapshot from Open-Meteo; its three facets are views of the same snapshot, not independently fetched data. A Forecast newly retrieved from Open-Meteo is distinct from the Last Known Forecast served from cache.

## Hourly Forecast
The near-term, hour-by-hour facet of a Forecast over the coming hours. Finer-grained than the Daily Forecast and forward-looking, unlike Current Conditions.

## Last Known Forecast
The most recent cached Forecast *for a specific Location*, shown when a Refresh for that Location cannot reach Open-Meteo. The cache holds one per Location, so stale data is always the active Location's own data — never another city's forecast mislabelled. It is a real Forecast that has simply aged — surfaced with the time it was retrieved (e.g. "last known forecast from 14:32") so its age is explicit. Not a separate kind of data from a Forecast; the term marks that it is being served from cache rather than freshly retrieved. A Location never previously fetched has no Last Known Forecast — offline, it shows the empty state.

## Location
The active place a Forecast is for. The app tracks at most one Location at a time — there is no saved collection of places, and until the user chooses one there is no active Location and no Forecast is shown (the first-launch empty state). Set in one of two ways: as the Current Location (from GPS, an arbitrary coordinate) or by the user picking a Supported City from the City List. Setting a new Location replaces the previous one. The subject of every Forecast and Refresh — a Forecast is always a Forecast *of* the active Location.

## Supported City
A member of the City List — a fixed, curated set of major world cities (capped at roughly 20–30 for now) that the user can pick as the active Location. The only Locations selectable by hand; arbitrary places are reachable only via the Current Location (GPS). Distinct from the Current Location, which is an arbitrary GPS coordinate rather than a curated entry.

## Open-Meteo
The external weather service that is the sole source of Forecast data. Free and key-less. The origin of every freshly retrieved Forecast; when it is unreachable the app falls back to the Last Known Forecast.

## Refresh
The act of retrieving a current Forecast for the active Location from Open-Meteo, by whatever trigger. A Refresh is either *manual* — user-initiated, which includes both selecting a new Location and an explicit refresh action — or *scheduled* (triggered automatically on a timer). There is no separate term for the first fetch after a Location-change; it is simply a manual Refresh. A failed Refresh leaves the Last Known Forecast in place rather than clearing it.

## Relationships
- The app has **at most one** active **Location** at a time; on first launch there is **none**.
- A **Location** is set either as the **Current Location** (GPS, arbitrary coordinate) **or** as one **Supported City** from the City List — never both at once.
- A **Refresh** fetches **exactly one Forecast** for the active **Location** from **Open-Meteo**.
- A **Forecast** bundles **one** **Current Conditions**, **one** **Hourly Forecast**, and **one** **Daily Forecast** — three facets of the same snapshot.
- The cache holds **one Last Known Forecast per Location**; a Location never fetched has none.
- A failed **Refresh** falls back to that Location's **Last Known Forecast**, or — if there is none — the empty state.

## Example dialogue
> **Dev:** "User's on Tokyo, taps refresh, network's down — what shows?"
> **Domain expert:** "Tokyo's **Last Known Forecast**, stamped with when we last got it. It's a real **Forecast**, just aged."
> **Dev:** "And if they'd never opened Tokyo before?"
> **Domain expert:** "Then there's no **Last Known Forecast** for it — empty state. We never show London's data under a Tokyo heading."
> **Dev:** "Picking Tokyo from the dropdown — is that a **Refresh**?"
> **Domain expert:** "Yes, a manual one. Selecting a **Supported City** and tapping refresh are both manual triggers; the timer is the scheduled one."

## Flagged ambiguities
- **"Location" vs a saved list** — resolved: at most one *active* Location, no saved collection. A `Saved Location` term is deferred until a saved-places feature is actually planned.
- **Manual entry as free-text search** — resolved: manual entry is a pick from the curated City List (a **Supported City**), not a geocoded text search. No `Place Result` / candidate-picking term.
- **"Load" vs "Refresh"** — resolved: the first fetch after a Location-change is just a manual **Refresh**; no separate `Load` term.
- **Units (°C/°F, km/h/mph)** — fixed metric for now; `Units` is deferred until a preferences feature is planned, not minted speculatively.
