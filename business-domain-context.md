# Domain Glossary

The shared vocabulary of the Desktop Weather App. Definitions only — no implementation, no process. Related terms are linked by reuse so the language forms one web.

## Current Conditions
The present-moment facet of a Forecast: temperature, conditions, and related now-values for a Location. A facet of a Forecast, not a separate fetch — distinct from the Hourly Forecast (near-future) and Daily Forecast (multi-day).

## Current Location
The Location derived from the device's GPS position, as opposed to one the user enters by hand. One way of setting the active Location; the other is manual entry. Not a saved place — it tracks wherever the device is.

## Daily Forecast
The multi-day facet of a Forecast: one summary entry per day (typically high/low and conditions) over the forecast horizon. Distinct from the Hourly Forecast (finer-grained, near-term) and Current Conditions (now).

## Forecast
The complete weather picture for a single Location at a point in time, bundling Current Conditions, the Hourly Forecast, and the Daily Forecast. One Forecast is one coherent snapshot from Open-Meteo; its three facets are views of the same snapshot, not independently fetched data. A Forecast newly retrieved from Open-Meteo is distinct from the Last Known Forecast served from cache.

## Hourly Forecast
The near-term, hour-by-hour facet of a Forecast over the coming hours. Finer-grained than the Daily Forecast and forward-looking, unlike Current Conditions.

## Last Known Forecast
The most recent Forecast held in cache, shown when a Refresh cannot reach Open-Meteo. It is a real Forecast that has simply aged — surfaced with the time it was retrieved (e.g. "last known forecast from 14:32") so its age is explicit. Not a separate kind of data from a Forecast; the term marks that it is being served from cache rather than freshly retrieved.

## Location
The place a Forecast is for. Set either as the Current Location (from GPS) or by the user entering a place by hand. The subject of every Forecast and Refresh — a Forecast is always a Forecast *of* a Location.

## Open-Meteo
The external weather service that is the sole source of Forecast data. Free and key-less. The origin of every freshly retrieved Forecast; when it is unreachable the app falls back to the Last Known Forecast.

## Refresh
The act of retrieving a current Forecast for a Location from Open-Meteo. A Refresh is either *manual* (user-initiated) or *scheduled* (triggered automatically on a timer). A failed Refresh leaves the Last Known Forecast in place rather than clearing it.
