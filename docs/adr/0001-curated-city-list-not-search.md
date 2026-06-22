# Manual location selection is a curated city list, not a geocoded search

**Status:** accepted

Manual Location selection is a dropdown of a fixed, curated set of major world
cities (a **Supported City**, capped at ~20–30 for now), not a free-text search box.

We chose the curated list because it keeps the app simple and removes a whole
dependency and failure mode: a search box would require Open-Meteo's geocoding
API plus a candidate-disambiguation flow (many "Londons"), with its own error,
empty, and ambiguous-match states. Arbitrary places remain reachable via the
Current Location (GPS); the curated list covers the common manual case.

The trade-off is global coverage and flexibility — a user can't manually pick a
town that isn't on the list. If that limitation bites, the reversal is to add a
geocoded search, which reintroduces the geocoding dependency and disambiguation
flow this decision deliberately avoids.
