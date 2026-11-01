# PokeTeams — GH-200 lab project (handoff for Claude Code)

> Put this file in the **repo root**. Claude Code reads `CLAUDE.md` automatically.
> The full lab plan is in `LAB-GH200.md` (same folder). Read it before doing anything.

## Who and why

- **Malik** is a senior systems engineer (Azure, M365, MSP/healthcare background; holds AZ-104, AZ-204, AZ-400) studying for **GH-200: GitHub Actions**.
- This repo exists to **practice GitHub Actions**. The .NET app is only a vehicle. Keep it small and boring.
- He is a student in this: explain the *why*, in exam terms (name the GH-200 domain and the keyword involved).

## Your two jobs

1. **Part 0 (time-boxed, about 15 minutes):** scaffold the app to the spec below, make it build, test and run locally, and commit it.
2. **Phases 1–6:** be a **coach** for the GitHub Actions work, not a ghostwriter.

## Ground rules

- **Do not create or edit anything under `.github/`** unless Malik explicitly says "write it for me". He writes the workflows and actions himself; that *is* the lab.
- When he asks for help with Actions, escalate in this order: (1) point to the keyword or doc, (2) give a partial snippet, (3) give the full answer only if he asks.
- If a phase in `LAB-GH200.md` needs an app change, make it, keeping it minimal. No features beyond the spec unless asked.
- **Never** write secrets, client IDs, tenant IDs or subscription IDs into files. Never commit publish profiles or `.env` files.
- Don't over-explain Azure (he knows it); do explain the GitHub side.
- Respect the clock: if he's stuck for more than 5 minutes on one step, offer the answer and move on.

## App spec (Part 0)

**Stack:** .NET 10 (LTS). Run `dotnet --list-sdks`; if 10 isn't installed, use the newest installed LTS and tell him. Use a **Blazor Web App** with **Interactive Server** render mode. No database, no auth.

```
PokeTeams.sln
global.json                      # pin the SDK major the workflows will use
README.md                        # 1-paragraph description + "<!-- CI badge goes here (Phase 1) -->"
.gitignore                       # dotnet gitignore
src/PokeTeams.Core/              # class library, no UI
  Models/Pokemon.cs              # Id, Name, SpriteUrl, Types (list), Stats
  Models/BaseStats.cs            # Hp, Attack, Defense, SpecialAttack, SpecialDefense, Speed, Total (computed)
  PokeApi/PokeApiClient.cs       # typed HttpClient + IMemoryCache (24h), list + get-by-name
  Teams/Team.cs                  # max 6 members, no duplicates, Add/Remove return success/failure
  Teams/TeamAnalyzer.cs          # pure functions: stat totals, per-stat averages,
                                 # distinct types represented, weakest stat name
src/PokeTeams.Web/               # Blazor Web App (Interactive Server)
  Components/Pages/Home.razor    # list first 151 + search box, click a Pokémon to see stats, "Add to team"
  Components/TeamPanel.razor     # up to 6 cards, remove button, totals + "weakest stat" callout
  Services/TeamState.cs          # scoped service (one per circuit), wraps Team
  Program.cs                     # register services; map GET /healthz -> 200 "ok"
  footer                         # show AssemblyInformationalVersion (the CI pipeline sets it)
tests/PokeTeams.Core.Tests/      # xUnit
  TeamTests.cs                   # 7th member rejected, duplicate rejected, remove works
  TeamAnalyzerTests.cs           # totals, averages, weakest stat, type list
                                 # NO network calls: build Pokemon objects by hand. ~6-10 tests, under 5 s.
```

**PokeAPI notes** (https://pokeapi.co/, no key needed):

- List: `GET https://pokeapi.co/api/v2/pokemon?limit=151`
- One Pokémon: `GET https://pokeapi.co/api/v2/pokemon/{nameOrId}`
- Stats are `stats[].stat.name`: `hp`, `attack`, `defense`, `special-attack`, `special-defense`, `speed`. The value is `stats[].base_stat`.
- Sprite: `sprites.front_default`. Types: `types[].type.name`.
- Their fair-use policy asks clients to **cache** responses. Use IMemoryCache and don't hammer the API.

**UI:** the default template's Bootstrap is fine. Show simple stat bars. Nothing fancy.

**Done when:**

- [ ] `dotnet build PokeTeams.sln -c Release` succeeds
- [ ] `dotnet test PokeTeams.sln -c Release` passes, with no network access needed
- [ ] `dotnet run --project src/PokeTeams.Web` lets you build a team of 6
- [ ] `/healthz` returns `ok`
- [ ] Committed as `Part 0: app scaffold` and pushed to `main`

## Commands the workflows will use (keep the app compatible)

```bash
dotnet restore PokeTeams.sln
dotnet build   PokeTeams.sln -c Release --no-restore
dotnet test    PokeTeams.sln -c Release --no-build --logger "trx;LogFileName=results.trx" --results-directory TestResults
dotnet publish src/PokeTeams.Web -c Release -o out -p:InformationalVersion=1.0.<run_number>
```

The tests must pass on **both Linux and Windows** (Phase 2 runs a matrix). Avoid path separators and culture-sensitive formatting in tests.

## Azure facts (Phase 3, for reference)

- App Service on **Linux**, F1 (free) or B1. Runtime string `DOTNETCORE:10.0` (verify with `az webapp list-runtimes --os linux`).
- **Web sockets must be ON** (Blazor Server needs them).
- Deploys through **OIDC** (`azure/login`, using a federated credential on an Entra app registration) plus `azure/webapps-deploy` with a zip package. No publish profiles and no client secrets.

## Phase-by-phase: what you may be asked

| Phase | Malik builds | You might help with |
|---|---|---|
| 1 | First CI workflow | Explaining trx output, why tests fail in CI but not locally |
| 2 | Matrix, outputs, artifacts | Windows-only test failures (fix the *test*, not the workflow) |
| 3 | OIDC deploy to Azure | App won't start on App Service (check runtime and web sockets, `az webapp log tail`) |
| 4 | Composite and JavaScript actions | Node 24 syntax in `index.js` (no npm packages on purpose) |
| 5 | Reusable and manual workflows | Reading `workflow_call` errors |
| 6 | Hardening | Finding release commit SHAs for pinning |

## Progress (update as phases complete)

- [x] Part 0 — app scaffold (pushed 2026-11-01)
- [ ] Phase 1 — first CI
- [ ] Phase 2 — matrix, outputs, artifacts, troubleshooting
- [ ] Phase 3 — deploy to Azure with OIDC and an environment
- [ ] Phase 4 — composite and JavaScript actions
- [ ] Phase 5 — reusable and manual workflows
- [ ] Phase 6 — hardening and enterprise tour
- [ ] Wrap-up — Azure resources deleted
