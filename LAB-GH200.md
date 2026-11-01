# GH-200 Lab — PokeTeams CI/CD on Azure (3 hours)

Build a small Blazor Pokémon team builder, then grow its pipeline one layer at a time: CI → matrix and artifacts → OIDC deploy to Azure → your own actions → reuse → hardening.

Each phase gives you a working result before the next adds complexity. When a phase turns something on, it tells you **why** first.

**Ground rules**

- Stick to the time boxes. Steps marked ⭐ **stretch** are optional; skip them if you're behind.
- You write everything under `.github/`. The coding agent builds the app and coaches; `CLAUDE.md` tells it so.
- Each phase ends with **"Exam lens"**: the question numbers from your practice bank that the phase covers. Answer them right after the phase, while it's fresh.

---

## How the time maps to the exam

GH-200 skills outline (updated February 2026):

| Domain | Exam weight | Where you practice it | ~Minutes |
|---|---|---|---|
| 1. Author and manage workflows | 20–25% | Phases 1, 2, 5 | 45 |
| 4. Manage GitHub Actions for the enterprise | 20–25% | Phases 3, 5, 6 (secrets/vars scoping, reuse, runners, policies) | 30 |
| 2. Consume and troubleshoot workflows | 15–20% | Phases 2, 5 | 25 |
| 3. Author and maintain actions | 15–20% | Phase 4 | 25 |
| 5. Secure and optimize automation | 10–15% | Phases 3, 6 | 30 |

Domain 4 is the heaviest one you can't fully lab on a personal account: runner groups, enterprise policies and IP allow lists need an org or enterprise. Phase 6 tours those settings, and those questions are mostly **know-the-rule** questions.

## Timeline

| Clock | Block | Result at the end |
|---|---|---|
| 0:00–0:15 | **Part 0** — scaffold with the coding agent | App runs locally, pushed to GitHub |
| 0:15–0:40 | **Phase 1** — first CI workflow | Green build on every push or PR, job summary, badge |
| 0:40–1:05 | **Phase 2** — matrix, outputs, artifacts, troubleshooting | Linux + Windows tests, versioned zip artifact |
| 1:05–1:40 | **Phase 3** — deploy to Azure with OIDC and an environment | App live on App Service behind an approval gate |
| 1:40–2:05 | **Phase 4** — build your own actions | Composite action + JavaScript action in use |
| 2:05–2:25 | **Phase 5** — reuse | Reusable deploy workflow + manual deploy with inputs |
| 2:25–2:50 | **Phase 6** — harden + enterprise tour | Injection fixed, SHAs pinned, build attested |
| 2:50–3:00 | **Wrap-up** | Azure cleaned up, exam-lens review |

---

## Before you start the clock (not counted)

- [ ] **A new public GitHub repo**, e.g. `poketeams`. Why public: free unlimited Actions minutes, and **environment protection rules** (required reviewers) work on the free plan for public repos. Nothing secret ever goes in the repo.
- [ ] `gh` CLI signed in (`gh auth status`), `az` CLI signed in (`az account show`), .NET SDK installed, Claude Code installed.
- [ ] Copy `CLAUDE.md` and this file into the repo root.
- [ ] Have your question bank open, ready for the Exam lens checks.

**About action versions:** snippets use the current major tags as of November 2026 (`checkout@v6`, `upload-artifact@v7`, and so on). If one fails with "unable to resolve", open that action's **Releases** page and use the latest major. Checking that page is itself an exam skill, and Phase 6 has you pin SHAs anyway.

---

## Part 0 — Scaffold the app (15 min)

1. In the repo folder, start Claude Code and say: *"Read CLAUDE.md and LAB-GH200.md, then do Part 0."*
2. While it works, skim `CLAUDE.md` so you know what it was told not to touch.
3. Check its work against the **Done when** list in `CLAUDE.md`, then push to `main`.

There's no `.github/` folder yet. You're about to create it.

---

## Phase 1 — Your first CI workflow (25 min) · Domain 1

**Goal:** every push to `main` and every PR into `main` restores, builds and tests the app, and shows a readable summary.

### 1.1 The skeleton (5 min)

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
    paths-ignore: ['**.md']      # editing LAB/CLAUDE/README alone won't trigger a run
  pull_request:
    branches: [main]
  workflow_dispatch:              # adds a "Run workflow" button on the Actions tab

permissions:
  contents: read                  # least privilege for GITHUB_TOKEN; every unlisted scope = none

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - run: dotnet restore PokeTeams.sln
      - run: dotnet build PokeTeams.sln -c Release --no-restore
      - name: Test
        run: dotnet test PokeTeams.sln -c Release --no-build --logger "trx;LogFileName=results.trx" --results-directory TestResults
```

Commit and push. Watch it on the **Actions** tab.

**Why these three triggers:** `push` is CI on `main`. `pull_request` tests the *merge result* before you merge. `workflow_dispatch` lets you run it by hand. The file must live in `.github/workflows/`, and `workflow_dispatch` only shows its button once the file is on the default branch.

### 1.2 Cache NuGet packages (5 min)

Add this between `setup-dotnet` and `restore`:

```yaml
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages                                 # REQUIRED: what to save
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}   # REQUIRED: exact-match key
          restore-keys: nuget-${{ runner.os }}-                   # optional: fallback prefix match
```

Push twice. On the second run, open the cache step's log and look for **"Cache restored from key"**.

**Why:** a **cache** is for dependencies reused *across runs*. An **artifact** (Phase 2) is for files produced by a run and passed to other jobs or downloaded. The exam loves this distinction.

### 1.3 Talk to the runner: workflow commands and the job summary (10 min)

Wrap the restore step in a log group, and add a summary step at the end of the job:

```yaml
      - name: Restore
        run: |
          echo "::group::dotnet restore output"
          dotnet restore PokeTeams.sln
          echo "::endgroup::"
```

```yaml
      - name: Job summary
        if: always()                      # run even if tests failed: summaries matter most then
        run: |
          echo "::notice title=Build::Built ${GITHUB_SHA::7} on ${{ runner.os }}"
          COUNTERS=$(grep -o '<Counters [^>]*' TestResults/results.trx || echo "no test results")
          {
            echo "## PokeTeams CI"
            echo "| Item | Value |"
            echo "|---|---|"
            echo "| Commit | \`${GITHUB_SHA::7}\` |"
            echo "| Trigger | ${{ github.event_name }} |"
            echo "| Actor | ${GITHUB_ACTOR} |"
            echo "| Tests | \`${COUNTERS}\` |"
          } >> "$GITHUB_STEP_SUMMARY"
```

Push, then open the run's **Summary** page: you'll see the table plus a notice annotation.

**Why:** workflow commands (`::notice::`, `::warning::`, `::error::`, `::debug::`, `::group::`, `::add-mask::`) are how a step talks *to the runner*. Environment files (`$GITHUB_OUTPUT`, `$GITHUB_ENV`, `$GITHUB_STEP_SUMMARY`) are how a step passes data *forward*. You'll use both all lab.

Notice `GITHUB_SHA` and `GITHUB_ACTOR`: these **default environment variables** are always set. Also know `GITHUB_REF` and `GITHUB_REPOSITORY`.

### 1.4 Status badge (2 min)

In `README.md`, replace the placeholder with:

```markdown
![CI](https://github.com/<you>/poketeams/actions/workflows/ci.yml/badge.svg)
```

### ✅ Checkpoint

A green run, a cache hit on the second run, a summary table and a badge.

### 🎯 Exam lens — Phase 1

- Triggers and file location: **Q7, 9, 19, 20, 24, 33, 36, 39, 43, 66, 67**
- Workflow commands and default vars: **Q1, 2, 23, 42, 49, 71, 88, 91**
- `env` scopes and syntax: **Q10, 13, 15, 17, 18, 26**
- Caching: **Q75**
- `GITHUB_TOKEN`: **Q3, 16, 28**

---

## Phase 2 — Matrix, outputs, artifacts and troubleshooting (25 min) · Domains 1 + 2

**Goal:** test on Linux and Windows, break the pipeline on purpose and diagnose it, then produce a versioned build artifact.

### 2.1 Matrix + a deliberate failure (12 min)

Change the `test` job header to:

```yaml
  test:
    name: test (${{ matrix.os }})
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false            # let the other OS finish even if one fails
      matrix:
        os: [ubuntu-latest, windows-latest]
    defaults:
      run:
        shell: bash               # Windows defaults to pwsh; your summary step uses bash syntax
    steps:
      # ...existing steps...
```

Add this step right after checkout. It's a practice failure; you'll delete it at the end of 2.1:

```yaml
      - name: Fails on purpose (DELETE after 2.1)
        if: matrix.os == 'windows-latest' && github.run_attempt == 1
        run: |
          echo "::debug::run_attempt is ${{ github.run_attempt }}"
          echo "::error title=Practice failure::Windows leg fails on attempt 1 on purpose"
          exit 1
```

Push, then work through this:

1. Open the failed run. Find the red job and the failing step, and read the error annotation on the Summary page.
2. Click **Re-run jobs → Re-run failed jobs**, and tick **Enable debug logging**. Only the Windows leg re-runs. It passes because `run_attempt` is now 2, and your `::debug::` line appears.
3. Pull the logs with the REST endpoint you saved:
   ```bash
   gh run list --workflow ci.yml --limit 3
   gh api repos/{owner}/{repo}/actions/runs/<RUN_ID>/logs > run-logs.zip   # gh fills {owner}/{repo}
   gh run view <RUN_ID> --log-failed                                       # quicker for just failures
   ```
4. **Persistent debug logging:** set a repository **variable** `ACTIONS_STEP_DEBUG=true` (Settings → Secrets and variables → Actions → Variables). Run the workflow with **Run workflow**, look at the extra output, then **delete the variable** because it's noisy. Add `ACTIONS_RUNNER_DEBUG` to that list for runner diagnostic logs. Exam answers usually say "set as a secret"; secrets and variables both work.
5. Delete the "Fails on purpose" step.

**Why:** `fail-fast: false` plus "re-run failed jobs" is the standard way to analyse failures across matrix variants. Debug logging has two levels: **step** debug (`ACTIONS_STEP_DEBUG`) and **runner** diagnostics (`ACTIONS_RUNNER_DEBUG`).

### 2.2 Publish job: outputs, `GITHUB_ENV` and artifacts (10 min)

Add a second job:

```yaml
  publish:
    needs: test                     # waits for ALL matrix legs to succeed
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.ver.outputs.version }}   # job output, readable by later jobs via needs.publish.outputs.version
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - id: ver
        run: |
          echo "version=1.0.${{ github.run_number }}" >> "$GITHUB_OUTPUT"   # step output: other steps/jobs
          echo "APP_VERSION=1.0.${{ github.run_number }}" >> "$GITHUB_ENV"   # env var: later steps in THIS job only
      - run: dotnet publish src/PokeTeams.Web -c Release -o out -p:InformationalVersion=$APP_VERSION
      - run: cd out && zip -r ../poketeams.zip .
      - uses: actions/upload-artifact@v7
        with:
          name: poketeams-web
          path: poketeams.zip
          retention-days: 5           # overrides the repo default (90) for this artifact
      - run: echo "Published **$APP_VERSION**" >> "$GITHUB_STEP_SUMMARY"
```

Push. Find the artifact at the bottom of the run's Summary page, then download it from the CLI:

```bash
gh run download <RUN_ID> -n poketeams-web
```

**Why:** each job runs on a fresh machine. **Outputs** carry small strings between jobs, and **artifacts** carry files. This is your saved artifacts sample, updated from `@v2` (retired) to the current major. Also check **Settings → Actions → General → Artifact and log retention**. The study guide says retention can also be set "via REST APIs", so take 1 minute to find that endpoint in the docs.

### 2.3 ⭐ Stretch — YAML anchors (3 min)

`test` and `publish` both repeat the `setup-dotnet` step. Anchor it once and reuse it within the same file:

```yaml
      - &setup-dotnet                 # in test: define the anchor
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
```

```yaml
      - *setup-dotnet                 # in publish: reuse it
```

Anchors only work **inside one file**. Phase 4's composite action solves the same problem **across files and repos**.

### ✅ Checkpoint

Two green matrix legs, a `publish` job with a `poketeams-web` artifact, and you've read logs in the UI, through the API, and with debug on.

### 🎯 Exam lens — Phase 2

- Matrix and conditionals: **Q11, 56**
- Passing data, artifacts vs cache: **Q40, 54, 59, 60, 115**
- Logs, debug, run management: **Q44, 51, 52, 53, 99**
- Storage limits: **Q45**

---

## Phase 3 — Deploy to Azure with OIDC and an environment (35 min) · Domains 5 + 4 + 1

**Goal:** a push to `main` deploys to App Service only after you approve it, with no long-lived Azure secret anywhere.

### 3.1 Azure resources (7 min)

You know this part. Copy, set `APP` to something globally unique, and run:

```bash
RG=rg-poketeams-lab
LOC=eastus
APP=poketeams-<initials><4digits>

az group create -n $RG -l $LOC
az appservice plan create -g $RG -n plan-poketeams --is-linux --sku F1
az webapp list-runtimes --os linux | grep -i dotnet          # confirm DOTNETCORE:10.0 exists
az webapp create -g $RG -p plan-poketeams -n $APP --runtime "DOTNETCORE:10.0"
az webapp config set -g $RG -n $APP --web-sockets-enabled true   # Blazor Server needs these
```

### 3.2 OIDC trust: GitHub ↔ Entra (8 min)

**Why OIDC:** instead of storing a client secret in GitHub, the workflow asks GitHub for a short-lived **OIDC token**, which needs `permissions: id-token: write`. Azure trusts that token because of a **federated credential** that matches the token's `subject`. Nothing long-lived is stored, so there's nothing to rotate or leak.

```bash
APP_ID=$(az ad app create --display-name gh-poketeams-lab --query appId -o tsv)
az ad sp create --id $APP_ID

# Subject = only jobs that run in the "production" environment of THIS repo can log in
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "gh-env-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<you>/poketeams:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Least privilege: deploy rights on this one web app only
WEBAPP_ID=$(az webapp show -g $RG -n $APP --query id -o tsv)
az role assignment create --assignee $APP_ID --role "Website Contributor" --scope $WEBAPP_ID
```

### 3.3 GitHub environment, secrets and variables (8 min)

**In the UI:** Settings → **Environments** → New environment → `production`:

- **Required reviewers:** add yourself. This is the approval gate.
- **Deployment branches and tags:** Selected branches → `main`.
- Optional: a **wait timer** of 1 minute, just to see it. Reviewers and wait timers are the two built-in protection rules.

**From the CLI** (watch the scoping):

```bash
# ENVIRONMENT secrets: only jobs that declare environment: production can read these
gh secret set AZURE_CLIENT_ID       --env production --body "$APP_ID"
gh secret set AZURE_TENANT_ID       --env production --body "$(az account show --query tenantId -o tsv)"
gh secret set AZURE_SUBSCRIPTION_ID --env production --body "$(az account show --query id -o tsv)"

# REPOSITORY variable: not sensitive, readable by any job via the vars context
gh variable set AZURE_WEBAPP_NAME --body "$APP"
```

**Why this split:**

- **Secrets** are encrypted and masked in logs. **Variables** are plain text, for config.
- The three IDs aren't truly secret, but the convention is to store them as secrets.
- Scopes go org → repo → environment. A more specific scope wins when names collide.

### 3.4 Deploy job (8 min)

Add a third job to `ci.yml`:

```yaml
  deploy:
    needs: publish
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'   # never deploy from PRs
    runs-on: ubuntu-latest
    environment:
      name: production
      url: ${{ steps.deploy.outputs.webapp-url }}       # shows as a link on the run graph
    concurrency:
      group: deploy-production
      cancel-in-progress: false     # queue deploys, don't kill one mid-flight
    permissions:
      id-token: write               # request the OIDC token
      contents: read
    steps:
      - uses: actions/download-artifact@v7
        with:
          name: poketeams-web
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - id: deploy
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ vars.AZURE_WEBAPP_NAME }}
          package: poketeams.zip
      - name: Smoke test
        env:
          URL: ${{ steps.deploy.outputs.webapp-url }}
          VERSION: ${{ needs.publish.outputs.version }}
        run: |
          curl --fail --retry 6 --retry-delay 10 --retry-all-errors "$URL/healthz"
          echo "Deployed **$VERSION** to $URL" >> "$GITHUB_STEP_SUMMARY"
```

**Why job-level `permissions`:** it overrides the workflow-level `contents: read`, and every scope you don't list becomes `none`. That's why `contents: read` is repeated here.

### 3.5 Ship it (4 min)

1. Push. The run reaches `deploy` and shows **Waiting**: that's the gate. A run left waiting is failed after **30 days**.
2. Click **Review deployments → production → Approve**.
3. Open the URL. The footer should show `1.0.<run_number>`.

If the site errors, run `az webapp log tail -g $RG -n $APP`, and ask the coding agent; this is one of the things it's briefed on.

### ✅ Checkpoint

The app is live behind an approval gate, and no Azure secret is stored in GitHub.

### 🎯 Exam lens — Phase 3

- Environments and approvals: **Q46, 50, 62, 122**
- Concurrency: **Q48**
- Secret and variable scoping: **Q21, 32, 78, 103, 105, 108, 117, 119, 120, 121**
- Chaining workflows: **Q14** (`workflow_run`, the alternative to `needs` across separate workflows)
- Large secrets: **Q29**

---

## Phase 4 — Build your own actions (25 min) · Domain 3

**Goal:** stop repeating setup steps (composite action), and write a real JavaScript action, so the action-type questions become obvious.

### 4.1 Composite action: "setup + cache + restore" in one step (10 min)

This is the "combine multiple workflow steps into one action" topic you saved.

Create `.github/actions/dotnet-setup/action.yml`:

```yaml
name: 'Set up .NET with NuGet cache'
description: 'setup-dotnet + NuGet cache + restore, as one step'
inputs:
  solution:
    description: 'Solution file to restore'
    required: false
    default: 'PokeTeams.sln'
outputs:
  cache-hit:
    description: 'true if NuGet packages came from cache'
    value: ${{ steps.cache.outputs.cache-hit }}   # composite outputs MUST map a value
runs:
  using: composite
  steps:
    - uses: actions/setup-dotnet@v5
      with:
        global-json-file: global.json
    - id: cache
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
        restore-keys: nuget-${{ runner.os }}-
    - run: dotnet restore "$SOLUTION"
      shell: bash                   # REQUIRED on every run step in a composite
      env:
        SOLUTION: ${{ inputs.solution }}   # pass inputs via env, not ${{ }} inside run (Phase 6 shows why)
```

In `ci.yml`, replace setup-dotnet + cache + restore in **both** jobs with:

```yaml
      - uses: actions/checkout@v6               # still needed: ./ paths resolve against the checkout
      - id: setup
        uses: ./.github/actions/dotnet-setup
      - run: echo "Cache hit? ${{ steps.setup.outputs.cache-hit }}"
```

If you did the anchors stretch, remove the anchor now. Push, and confirm it's still green.

### 4.2 JavaScript action: "Pokémon of the build" (12 min)

It picks a Pokémon from the run number, writes it to the job summary, and sets an output. There are **no npm packages on purpose**, so you can see what `@actions/core` does under the hood.

Create `.github/actions/pokemon-of-the-build/action.yml`:

```yaml
name: 'Pokémon of the build'
description: 'Picks a Gen-1 Pokémon from a number and adds it to the job summary'
inputs:
  seed:
    description: 'Any whole number (the run number works well)'
    required: true
outputs:
  pokemon:
    description: 'Name of the chosen Pokémon'
runs:
  using: node24          # JavaScript action = runs.using: nodeXX
  main: index.js         # entry point; optional pre: / post: scripts also exist
```

Create `.github/actions/pokemon-of-the-build/index.js`:

```js
// No dependencies: this is roughly what @actions/core does for you.
const fs = require('fs');

async function run() {
  // Inputs arrive as environment variables: INPUT_<NAME IN UPPER CASE>
  const seed = parseInt(process.env.INPUT_SEED, 10);
  if (Number.isNaN(seed)) throw new Error(`seed must be a number, got "${process.env.INPUT_SEED}"`);
  const id = ((seed - 1) % 151 + 151) % 151 + 1;

  const res = await fetch(`https://pokeapi.co/api/v2/pokemon/${id}`);
  if (!res.ok) throw new Error(`PokeAPI returned HTTP ${res.status}`);
  const p = await res.json();
  const total = p.stats.reduce((sum, s) => sum + s.base_stat, 0);

  // core.setOutput()  ==  append name=value to the GITHUB_OUTPUT file
  fs.appendFileSync(process.env.GITHUB_OUTPUT, `pokemon=${p.name}\n`);

  // core.summary  ==  append Markdown to GITHUB_STEP_SUMMARY
  fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY,
    `### Pokémon of the build: ${p.name} (#${id})\n\n` +
    `![${p.name}](${p.sprites.front_default})\n\nBase stat total: **${total}**\n`);

  // core.notice()  ==  a workflow command on stdout
  console.log(`::notice::Pokémon of the build is ${p.name}`);
}

run().catch(err => {
  console.log(`::error::${err.message}`);   // core.setFailed() == error message + ...
  process.exitCode = 1;                     // ... a non-zero exit code = failed step
});
```

Use it in the `publish` job:

```yaml
      - id: pkmn
        uses: ./.github/actions/pokemon-of-the-build
        with:
          seed: ${{ github.run_number }}
      - run: echo "This build's mascot is ${{ steps.pkmn.outputs.pokemon }}"
```

Push, and check the sprite on the Summary page. Then **break it on purpose**: set `seed: pikachu`, push, read the error annotation, and revert.

### 4.3 Read, don't build: the other things Domain 3 asks (3 min)

- **Three action types:**
  - **JavaScript:** `runs.using: node24`. Fastest, and runs on any OS.
  - **Docker container:** `runs.using: docker`, needs a `Dockerfile` next to `action.yml`. **Linux runners only.**
  - **Composite:** `runs.using: composite`. Bundles steps.
- **Metadata:** every action needs `action.yml` (or `action.yaml`). It defines `inputs`, `outputs` and `runs`. `pre:` and `pre-if:` run before `main`; `post:` runs after.
- **Distribution:**
  - Same repo: `./path`.
  - Another repo: `owner/repo@ref`.
  - **Marketplace:** a **public** repo with a **single `action.yml` at the root** and a unique name.
  - Keep each action in its own repo so it can be versioned and released on its own.
- **Versioning:** publish semver tags (`v1.2.0`) and move a major tag (`v1`) along with them. Consumers pin the major for convenience, or the **commit SHA** for safety.

### ✅ Checkpoint

Both jobs use your composite action, and the summary shows your Pokémon.

### 🎯 Exam lens — Phase 4

- Action types: **Q68, 69, 74, 76, 89, 94, 96, 100**
- Metadata: **Q73, 83, 84, 90, 93, 95**
- JavaScript internals and errors: **Q70, 77, 82**
- Composite: **Q101**
- Versioning and distribution: **Q12, 72, 97, 98, 102, 104, 109, 113**

---

## Phase 5 — Reuse: reusable workflows and manual runs (20 min) · Domains 2 + 4 + 1

**Goal:** the deploy logic lives in one place. CI calls it, and a manual "deploy now" workflow with inputs calls it too.

### 5.1 Extract a reusable deploy workflow (8 min)

Create `.github/workflows/deploy-azure.yml`. It's the Phase 3 job, moved here behind `workflow_call`:

```yaml
name: Deploy to Azure (reusable)

on:
  workflow_call:
    inputs:
      environment:
        type: string          # workflow_call inputs: string | number | boolean
        required: true
      version:
        type: string
        required: true
      artifact-name:
        type: string
        default: poketeams-web
    outputs:
      url:
        value: ${{ jobs.deploy.outputs.url }}

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment:
      name: ${{ inputs.environment }}
      url: ${{ steps.deploy.outputs.webapp-url }}
    concurrency:
      group: deploy-${{ inputs.environment }}
      cancel-in-progress: false
    permissions:
      id-token: write
      contents: read
    outputs:
      url: ${{ steps.deploy.outputs.webapp-url }}
    steps:
      # paste the Phase 3 steps here, changing: name: ${{ inputs.artifact-name }}
      # and in the smoke test: VERSION: ${{ inputs.version }}
```

In `ci.yml`, replace the whole `deploy` job with a **caller job**:

```yaml
  deploy:
    needs: publish
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    permissions:
      id-token: write         # the CALLER must grant it; a called workflow can only reduce permissions
      contents: read
    uses: ./.github/workflows/deploy-azure.yml     # no runs-on or steps in a caller job
    with:
      environment: production
      version: ${{ needs.publish.outputs.version }}
```

**Why no `secrets:` here:** your Azure IDs are **environment secrets**. A called workflow job that declares `environment:` reads that environment's secrets directly; the caller can't pass environment secrets. For **repo or org** secrets you'd add `secrets: inherit`, or map them one by one.

Push, approve, and confirm the deploy still works.

### 5.2 Manual deploy with typed inputs (7 min)

Create `.github/workflows/manual-deploy.yml`:

```yaml
name: Manual deploy
run-name: Manual deploy to ${{ inputs.environment }} by @${{ github.actor }}

on:
  workflow_dispatch:
    inputs:
      environment:
        description: Target environment
        type: environment       # dispatch-only type: a dropdown of your environments
        required: true
      run-tests:
        description: Run tests first?
        type: boolean
        default: true
      reason:
        description: Why are you deploying?
        type: string
        required: false

permissions:
  contents: read

jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.ver.outputs.version }}
    steps:
      - uses: actions/checkout@v6
      - uses: ./.github/actions/dotnet-setup
      - if: inputs.run-tests            # the inputs context keeps booleans as booleans
        run: dotnet test PokeTeams.sln -c Release
      - id: ver
        run: echo "version=1.0.${{ github.run_number }}-manual" >> "$GITHUB_OUTPUT"
      - run: dotnet publish src/PokeTeams.Web -c Release -o out -p:InformationalVersion=${{ steps.ver.outputs.version }}
      - run: cd out && zip -r ../poketeams.zip .
      - uses: actions/upload-artifact@v7
        with:
          name: poketeams-web
          path: poketeams.zip
          retention-days: 1
      - name: Record reason
        env:
          REASON: ${{ inputs.reason }}  # free text from a human = untrusted: env, never inline
        run: echo "Reason: ${REASON:-none given}" >> "$GITHUB_STEP_SUMMARY"

  deploy:
    needs: build
    permissions:
      id-token: write
      contents: read
    uses: ./.github/workflows/deploy-azure.yml
    with:
      environment: ${{ inputs.environment }}          # dispatch input -> workflow_call input
      version: ${{ needs.build.outputs.version }}
```

Run it from the CLI:

```bash
gh workflow run manual-deploy.yml -f environment=production -f run-tests=false -f reason="dispatch practice"
```

This exercises the exact study-guide bullet: *define `workflow_dispatch` inputs (types, required, defaults) and pass inputs to reusable workflows via `workflow_call`*.

### 5.3 Disable vs delete (2 min)

```bash
gh workflow disable "Manual deploy"
gh workflow run manual-deploy.yml -f environment=production   # fails: the workflow is disabled
gh run list --workflow manual-deploy.yml                       # history is still there
gh workflow enable "Manual deploy"
```

**Disable** keeps the file and the run history and stops triggers. **Delete** means removing the YAML file.

### 5.4 Fill this in yourself, no peeking (3 min)

| | Starter workflow (template) | Reusable workflow | Composite action |
|---|---|---|---|
| Unit of reuse | | | |
| How it's invoked | | | |
| Stays linked to the source after use? | | | |
| Where it lives for an org | | | |
| Can it set `runs-on` / environments? | | | |

### ✅ Checkpoint

CI deploys through the reusable workflow, a manual deploy works with inputs, and you've disabled and re-enabled a workflow.

### 🎯 Exam lens — Phase 5

- Reusable workflows: **Q58, 64, 105, 114**
- Templates and reuse strategy: **Q65, 111, 116**
- Disable vs delete: **Q34, 57**
- Where actions can be referenced from: **Q8, 41, 107**

---

## Phase 6 — Harden it + enterprise tour (25 min) · Domains 5 + 4

### 6.1 Script injection: exploit it, fix it, then use an action instead (10 min)

Create `.github/workflows/pr-check.yml`. It's **insecure on purpose**:

```yaml
name: PR title check
on:
  pull_request:
    types: [opened, edited, synchronize]
permissions:
  contents: read
jobs:
  title:
    runs-on: ubuntu-latest
    steps:
      - name: Check title (INSECURE)
        run: echo "Checking: ${{ github.event.pull_request.title }}"
```

Push it to `main`. Then create a branch with any small change and open a PR titled:

```
feat: add Snorlax"; ls -la; echo "
```

Open the run log: a **directory listing** printed. Your PR title was **executed as shell**, because `${{ }}` is pasted into the script *before* bash runs it.

**Fix:** put the title in an environment variable, and add a real rule plus a bot comment through **actions/github-script**:

```yaml
jobs:
  title:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write          # only what's needed to comment
    steps:
      - name: Check title (safe)
        env:
          TITLE: ${{ github.event.pull_request.title }}   # data, not code
        run: |
          echo "Checking: $TITLE"
          if [[ ! "$TITLE" =~ ^(feat|fix|docs|ci|chore): ]]; then
            echo "::error::PR title must start with feat:, fix:, docs:, ci: or chore:"
            exit 1
          fi
      - name: Team Rocket bot comment
        if: failure()
        uses: actions/github-script@v8
        with:
          script: |
            await github.rest.issues.createComment({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.issue.number,
              body: 'Team Rocket bot: please use a conventional title, e.g. `feat: add team export`.'
            })
```

Push the fix to `main`. Edit the PR title to `bad title`: it fails and the bot comments. Change it to `feat: add Snorlax`: it passes.

**Why it matters:**

- `github-script` gets an authenticated client from `GITHUB_TOKEN` automatically.
- PRs from **forks** get a read-only token and no secrets.
- `pull_request_target` runs with write access and secrets, so never check out PR code in it.

### 6.2 Pin to SHAs + let Dependabot maintain them (6 min)

Resolve a tag to a commit SHA:

```bash
gh api repos/actions/checkout/commits/v6 --jq .sha     # use the full 40-char SHA
```

Pin at least `actions/checkout` and `azure/login`, keeping the version as a trailing comment:

```yaml
- uses: actions/checkout@<40-char-sha>   # v6.x.y
```

Create `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: github-actions
    directories: ["/", "/.github/actions/*"]     # include your composite and JS actions
    schedule:
      interval: weekly
```

**Why:** a tag can be moved by whoever controls that repo; a SHA can't. Dependabot keeps SHA pins current and updates the trailing comment.

### 6.3 Build provenance attestation (5 min)

Give `publish` the permissions it needs, and attest the zip:

```yaml
  publish:
    permissions:
      contents: read
      id-token: write          # sign with an OIDC-backed identity
      attestations: write      # store the attestation
    steps:
      # ...after the zip step:
      - uses: actions/attest-build-provenance@v3
        with:
          subject-path: poketeams.zip
```

Push, then verify locally:

```bash
gh run download <RUN_ID> -n poketeams-web
gh attestation verify poketeams.zip -R <you>/poketeams
```

This proves *this exact zip* came from *this workflow in this repo*. That's SLSA build provenance.

### 6.4 Enterprise settings tour (4 min, read only)

Open each of these and say out loud what it controls:

- **Repo Settings → Actions → General:**
  - Allowed actions: all / owner + verified / allow-list patterns
  - Fork PR approval
  - **Workflow permissions** (`GITHUB_TOKEN` default: read vs read/write)
  - Whether Actions can create or approve PRs
  - Artifact and log retention
- **Repo Settings → Actions → Runners → New self-hosted runner:** read the `config.sh --labels` step.
- **If you have an org:**
  - Org secrets with **selected repositories** access
  - **Runner groups**, which control which repos can use which runners
  - Allowed-actions policy at org level
- **Know without seeing:**
  - **IP allow lists:** GitHub-hosted runners are blocked unless you allow their ranges, or use self-hosted or larger runners with static IPs.
  - **Workflow templates** live in the org's `.github` repo under `workflow-templates/`, as a `.yml` plus a `.properties.json`.
  - **Self-hosted runner labels:** `runs-on: [self-hosted, linux, x64, gpu]` needs **all** of them to match.

### ⭐ Stretch — a self-hosted runner on your machine (10 min, only if ahead)

> ⚠️ **Self-hosted runners on public repos are risky:** any PR could run code on your machine. Use a `workflow_dispatch`-only workflow and **remove the runner as soon as you're done**.

1. Settings → Actions → Runners → **New self-hosted runner**, follow the commands, and add the label `pokelab`.
2. Create a workflow with only `on: workflow_dispatch` and `runs-on: [self-hosted, pokelab]` that runs `dotnet --info`. Run it.
3. Stop the runner and run `./config.sh remove --token <token from UI>`.

### ✅ Checkpoint

Injection demonstrated and fixed, SHAs pinned, the zip attested, and you can explain every Actions setting page.

### 🎯 Exam lens — Phase 6

- Trusted actions and policies: **Q25, 81, 92, 112, 124**
- Runners: **Q4, 5, 6, 30, 31, 37, 38, 86, 106, 110, 123**
- Networking and GHES: **Q79, 85**

---

## Wrap-up (10 min)

**1. Clean up Azure.** The F1 plan is free, but don't leave identities around:

```bash
az group delete -n rg-poketeams-lab --yes --no-wait
az ad app delete --id $APP_ID
```

**2. Say these out loud without looking.** If you stumble on one, that's your review topic:

1. Cache vs artifact: when to use each, and which inputs `actions/cache` requires.
2. `$GITHUB_OUTPUT` vs `$GITHUB_ENV` vs `$GITHUB_STEP_SUMMARY`.
3. The three action types, the file each needs, and which one is Linux-only.
4. Why the caller job needs `id-token: write` and the called workflow can't add it.
5. Why environment secrets didn't need `secrets:` on the caller.
6. Starter workflow vs reusable workflow vs composite action.
7. The two environment protection rules, and how long a run can wait.
8. Three ways to stop script injection.

**3. Update `CLAUDE.md` → Progress** so the next session knows where you stopped.

## If you finish early (bonus, not timed)

- **Packages:** build a container image and push it to **GHCR** with `docker/login-action` and `GITHUB_TOKEN` (`packages: write`). Covers **Q87**.
- **Service containers:** a job with `services: redis:` that pings Redis on `localhost:6379`. Covers **Q5** and the `services:` bullet.
- **Release:** on tag `v*`, create a GitHub release with the zip attached. Covers **Q35**.
- **Warm-up repo:** github.com/skills/hello-github-actions

---

## Your saved links, mapped to phases

| Link | Phase |
|---|---|
| [Workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) | All |
| [Events that trigger workflows](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows) | 1, 5 |
| [MS Learn — configure a workflow](https://learn.microsoft.com/en-us/training/modules/github-actions-automate-tasks/2c-configure-github-actions-workflow) | 1 |
| [MS Learn — essential features](https://learn.microsoft.com/en-us/training/modules/github-actions-ci/github-actions-essential-features) | 1, 2 |
| [MS Learn — cache, share, debug](https://learn.microsoft.com/en-us/training/modules/github-actions-ci/cache-share-debug-workflows) | 1, 2 |
| [MS Learn — manage workflow runs](https://learn.microsoft.com/en-us/training/modules/github-actions-cd/2b-manage-workflow-runs) | 2, 5 |
| [MS Learn — deploy to Azure](https://learn.microsoft.com/en-us/training/modules/github-actions-cd/2-deploy-azure) | 3 |
| [Use variables](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-variables) | 3 |
| [MS Learn — encrypted secrets](https://learn.microsoft.com/en-us/training/modules/manage-github-actions-enterprise/manage-encrypted-secrets) | 3 |
| [Control workflow concurrency](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency) | 3 |
| [MS Learn — create custom actions](https://learn.microsoft.com/en-us/training/modules/create-custom-github-actions/create-custom-github-action) | 4 |
| [Reuse workflows](https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows) | 5 |
| [MS Learn — reusable actions (enterprise)](https://learn.microsoft.com/en-us/training/modules/manage-github-actions-enterprise/manage-leverage-reusable-actions) | 5 |
| [MS Learn — manage actions & workflows (enterprise)](https://learn.microsoft.com/en-us/training/modules/manage-github-actions-enterprise/manage-actions-workflows) | 5, 6 |
| [MS Learn — github-script](https://learn.microsoft.com/en-us/training/modules/automate-github-using-github-script/3-use-github-script) | 6 |
| [Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use#using-third-party-actions) | 6 |
| [MS Learn — publish packages](https://learn.microsoft.com/en-us/training/modules/github-actions-packages/2b-publish-using-workflow) | Bonus |
| [hello-github-actions](https://github.com/skills/hello-github-actions) | Bonus |
