# Desktop Weather App — agent front door

This product is built with the **Enate SDLC Factory**. Start here:
https://github.com/kitcox-dev/enate-claude-skills/blob/main/docs/using-the-sdlc-factory.md

## Documentation fabric

- `CONTRIBUTING.md` — branching & merge rules.
- `Technical-Context.MD` — engineering contract: principles, packages-in-use, the Testing & ratchet standard. _(written by `/init-tech-context`)_
- `business-domain-context.md` — the project's domain glossary. _(written by `/init-context`)_

## Dev commands

Stack: .NET 8 + Avalonia 11 + CommunityToolkit.Mvvm. See `Technical-Context.MD` for the full contract.

- Restore: `dotnet restore`
- Build: `dotnet build`
- Run: `dotnet run --project src/DesktopWeatherApp` _(project path lands when the scaffold does)_
- Test (Tier 1): `dotnet test`
- Format check: `dotnet format --verify-no-changes`
- Format apply: `dotnet format`

Tier 2 / Tier 3 commands land alongside the scaffolds that introduce them.
