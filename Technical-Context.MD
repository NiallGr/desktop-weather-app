# Technical Context

Engineering-practice contract for code-writing agents in this project. Every implementation must respect what's written here. If a constraint conflicts with the user's request, flag the conflict — don't silently bypass it.

## Overriding Principles
1. **Weather-API keys live in OS-secure storage only.** Windows-only target today uses `Microsoft.Win32.Credentials`; cross-platform keyring is an ADR when we expand beyond Windows. Trivially satisfied today (Open-Meteo needs no key) — the precedent is the point.
2. **The view ↔ service boundary is a typed contract.** Every service exposed to view-models has explicit types end-to-end; no `dynamic`, no `object` across the boundary. Breaking the contract requires an ADR.
3. **No silent network calls from the view layer.** Views and view-models never reach Open-Meteo (or any API) directly — all outbound HTTP goes through a service layer so we can log, cache, and rate-limit in one place.

## Frameworks
- **.NET 8** — runtime.
- **Avalonia UI 11** — cross-platform desktop UI (XAML, MVVM).
- **CommunityToolkit.Mvvm** — source-generator-driven MVVM (no boilerplate `INotifyPropertyChanged`).

## 3rd-party tech
- **Open-Meteo** — weather forecast data source. Free, no API key, ECMWF/GFS-grade forecasts. Sole external dependency.

## Packages in use
**HTTP:** `System.Net.Http.HttpClient` via `IHttpClientFactory` (built-in).
**JSON:** `System.Text.Json` (built-in; Newtonsoft only with a written reason).
**Credentials:** `Microsoft.Win32.Credentials` via a thin wrapper (Windows-only initial target).
**Logging:** `Microsoft.Extensions.Logging` + `Serilog` (file + Debug sinks).
**DI:** `Microsoft.Extensions.DependencyInjection`.
**Date/time:** `NodaTime` — use `Instant` / `ZonedDateTime`, never raw `DateTime` for forecast timestamps.
**Testing:** `xUnit`, `FluentAssertions`, `Avalonia.Headless.XUnit`. `FsCheck.Xunit` for property-based tests where the domain suits it.
**E2E:** `Appium` for true end-to-end on Windows (Tier 2/3).
**Lint/format:** `dotnet format` + `.editorconfig` with nullable + analyzer rules as warnings-as-errors.

## Instrumentation
Logger is `Serilog` via `Microsoft.Extensions.Logging`. Sinks: rolling file at `%LOCALAPPDATA%\DesktopWeatherApp\logs\app-.log` (one file per day, 7-day retention) using `CompactJsonFormatter`; Debug sink in DEBUG builds (human-readable). No console sink — desktop app, end users don't have one. Enrichers always attach `Application`, `Version`, `MachineName`, `CorrelationId`. Default level `Information`; `Microsoft.*` and `System.*` at `Warning`; `Debug` only in DEBUG builds.

**Always logged (Information+):** every outbound Open-Meteo call (URL sans secrets, status, latency, response size, correlation id); every refresh trigger (manual / scheduled) with the resolved location key; every unhandled exception at `Error` (wired via `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`). Cache hit/miss at Debug to keep volume sane.

**Never logged:** full HTTP response bodies; GPS coordinates at fine precision (round to ~0.1° before logging); any future API keys.

**Telemetry / metrics / traces:** none. Adding any is an explicit opt-in per Overriding Principle #3 and requires an ADR.

## User Feedback Approach
Single-user desktop app. Primary channel is the in-app UI — non-blocking toast/snackbar in the main window for errors and warnings; modal dialogs reserved for actions that need confirmation (e.g. clearing cached data). Tone is terse and factual, no apologies. "Couldn't reach Open-Meteo — showing last known forecast from 14:32." Never "Oh no, something went wrong!" Never "We apologise for the inconvenience."

Stack traces never appear in the UI — they go to the log file; the user gets a short message plus a "Copy diagnostics" button that grabs the correlation id and the relevant log lines. Loading states are skeleton placeholders, not spinner overlays — the previous forecast stays visible (with a subtle "refreshing…" indicator) until the new one lands or fails. Every async view-model exposes `IsLoading`, `Error`, and `HasData`; the view binds to all three. Empty and error states are first-class, not happy-path-only.

Defaults for agent-generated copy: present tense, lowercase sentences acceptable for status text, no emoji unless explicitly requested.

## Branching and Environments
- Model: **trunk-based**. `main` is the only long-lived branch.
- Branches: HITL work on short-lived `<type>/<slug>` branches via PR; AFK orchestrator work on `story/<issue#>-<slug>` branches, one Story in flight at a time.
- Merge: **squash-only** into `main`, branch auto-deleted on merge.
- Environments: **local dev only** today. No staging / prod — the app is installed per machine. Release artefacts get an ADR when the first release is planned.
- Protected: `main` is **convention-only** (free private repo, server-side protection unavailable). Treat the convention as binding. See `CONTRIBUTING.md`.

## Git Guardrails
- **Never without explicit user confirmation:** `git push --force` / `--force-with-lease`, `git reset --hard`, `git clean -fd`, `git branch -D`, `git rebase` on `main`, branch deletion on the remote, `git commit --amend` on a pushed commit.
- **Never, full stop:** `--no-verify` on commits/pushes, bypassing signing flags, `git push` directly to `main` (always via PR even though protection is convention-only).
- **Hooks:** none yet. Run `/setup-pre-commit` once the C# scaffold lands (wires `dotnet format` + `dotnet test` on commit). Run `/git-guardrails-claude-code` to install Claude Code hooks that block the destructive list above before they execute.

## Testing & the ratchet
> Seeded from the Factory standard — **opt-out, not opt-in**. Trim a line only with a deliberate, recorded decision; don't start from nothing. Full standard: <https://github.com/kitcox-dev/enate-claude-skills/blob/main/docs/testing-and-the-ratchet-standard.md> · the *why*: <https://github.com/kitcox-dev/enate-claude-skills/blob/main/docs/agentic-e2e-testing-principles.md>.

- **Test the contract, not the LLM.** Assert on the deterministic envelope (output shape, state transition, side-effect ordering), never on generated text or chain-of-thought.
- **Every seam gets a real-IO test on at least one side.** Mock-on-both-sides only proves the mocks agree; defects live at integration seams, not inside modules.
- **Three tiers, each catching a class the others can't.** Tier 1 — recorded-replay (real local I/O, fakes at the seams, replayed Open-Meteo JSON fixtures via a fake `IHttpClientFactory`, every commit); Tier 2 — live-API, disposable (real subprocess of the built app against a live Open-Meteo call to a stable lat/lon, fresh temp `%LOCALAPPDATA%` per run, bounded by wall-clock minutes, scheduled); Tier 3 — live (the installed app on a real Windows desktop running its real schedule; assertions on log events + cached forecast files, never UI pixels; continuous).
- **Platform matrix is non-negotiable for OS-touching code.** Windows-first today, so Tier 1 + Tier 2 run on `windows-latest`. The day macOS/Linux ships, Tier 1 + Tier 2 expand to those runners.
- **Plan test coverage at the Feature level, not the Story level.** Each Feature declares at kickoff which tiers it needs, which fixtures/substrates must exist, and a Tier-2 cost ceiling.
- **The ratchet — every Tier-3 defect pins a cheap-tier regression.** A live defect's fix lands with a Tier-1 (or Tier-2) regression that would have caught it, encoding the **class** of defect **bounded by the seam** where it surfaced — not the exact reproducer, and not a project-wide sweep. A Story that surfaced from a live defect carries that ratcheting fixture as an acceptance criterion.
- **Disposable substrate, not shared.** Tier-2 substrate is created per run and torn down; reproducibility comes from a seed (fixture file/manifest/script), never persistent state.
- **Assert on observable behaviour, not internal state.** For Tier 2/3 the target is what an operator could verify from external artefacts — cached forecast files, log events, UI snapshots from `Avalonia.Headless`.
