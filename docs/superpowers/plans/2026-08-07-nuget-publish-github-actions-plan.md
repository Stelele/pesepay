# NuGet Publish via GitHub Actions — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a GitHub Actions workflow that builds, tests, and publishes Stelele.PesePay to nuget.org on non-prerelease Git tags.

**Architecture:** Single workflow file (`ci.yml`) with one job: build → unit tests → integration tests → pack → push. Publish steps are conditional on release tags. A new `global.json` pins the SDK and a new `PesePay.sln` ensures all projects build together.

**Tech Stack:** .NET 10.0, MinVer (versioning), GitHub Actions, nuget.org

---

### Task 1: Create `global.json`

**Files:**
- Create: `global.json`

- [ ] **Step 1: Write the file**

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 2: Verify it's recognized**

Run: `dotnet --version`
Expected: outputs `10.0.x` (matches the pinned version)

- [ ] **Step 3: Commit**

```bash
git add global.json
git commit -m "chore: add global.json to pin .NET SDK version"
```

---

### Task 2: Create `PesePay.sln`

**Files:**
- Create: `PesePay.sln`

- [ ] **Step 1: Scaffold and add projects**

```bash
dotnet new sln -n PesePay --force
dotnet sln PesePay.sln add PesePay.csproj
dotnet sln PesePay.sln add PesePay.Tests/PesePay.Tests.csproj
dotnet sln PesePay.sln add PesePay.Tests.Integration/PesePay.Tests.Integration.csproj
```

- [ ] **Step 2: Verify solution builds all projects**

Run: `dotnet build PesePay.sln -c Release`
Expected: Build succeeds, no errors. Output shows all three projects compiled (PesePay for net8.0/net9.0/net10.0 + both test projects for net10.0).

- [ ] **Step 3: Commit**

```bash
git add PesePay.sln
git commit -m "chore: add solution file linking library and test projects"
```

---

### Task 3: Update `PesePay.csproj` for NuGet publishing

**Files:**
- Modify: `PesePay.csproj`

- [ ] **Step 1: Remove hardcoded `<Version>`**

Delete this line:
```xml
    <Version>1.0.0</Version>
```

MinVer derives the version from Git tags at build time. The hardcoded version would conflict.

- [ ] **Step 2: Add MinVer tag prefix**

After `<RepositoryUrl>`, add:
```xml
    <MinVerTagPrefix>v</MinVerTagPrefix>
```

- [ ] **Step 3: Add symbol package support**

After `<MinVerTagPrefix>`, add:
```xml
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
```

- [ ] **Step 4: Add NuGet package metadata**

After `<SymbolPackageFormat>`, add:
```xml
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>pesepay;payment;gateway;zimbabwe;ecocash;visa;mastercard;zimswitch;paygo;omari;innbucks;mobile-money</PackageTags>
    <RepositoryType>git</RepositoryType>
```

- [ ] **Step 5: Add ItemGroup to include README in package**

Before the closing `</Project>` tag, add:
```xml
  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="" />
  </ItemGroup>
```

- [ ] **Step 6: Verify the project still builds**

Run: `dotnet build PesePay.sln -c Release`
Expected: Build succeeds. No errors.

- [ ] **Step 7: Verify packing produces expected artifacts**

Run: `dotnet pack PesePay.csproj -c Release -o ./nupkg`
Expected: Creates `nupkg/Stelele.PesePay.x.y.z.nupkg` and `nupkg/Stelele.PesePay.x.y.z.snupkg` (version from MinVer).

- [ ] **Step 8: Commit**

```bash
git add PesePay.csproj
git commit -m "feat: configure NuGet package metadata, symbols, and MinVer"
```

---

### Task 4: Guard `IAsyncLifetime` in integration tests

**Files:**
- Modify: `PesePay.Tests.Integration/PesePaySandboxTests.cs`

- [ ] **Step 1: Add guard to `InitializeAsync` in `SandboxSeamlessPaymentTests`**

Find the class `SandboxSeamlessPaymentTests` implementing `IAsyncLifetime`. Replace `InitializeAsync` with:

```csharp
    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(SandboxCredentials.IntegrationKey) ||
            string.IsNullOrEmpty(SandboxCredentials.EncryptionKey))
            return;

        _client = SandboxCredentials.CreateClientWithUrls();

        var usdResult = await _client.GetPaymentMethodsAsync("USD");
        if (usdResult.IsSuccess) _usdMethods = usdResult.Data!;

        var zigResult = await _client.GetPaymentMethodsAsync("ZiG");
        if (zigResult.IsSuccess) _zigMethods = zigResult.Data!;
    }
```

The guard must be **before** any call to `SandboxCredentials.CreateClientWithUrls()`, which dereferences the env vars and throws `NullReferenceException` when they are missing. In xUnit, `IAsyncLifetime.InitializeAsync` runs even for tests that `[SandboxFact]` would skip.

- [ ] **Step 2: Verify tests still pass (with secrets)**

If sandbox secrets are configured in your environment, run:
```bash
dotnet test PesePay.Tests.Integration/PesePay.Tests.Integration.csproj -c Release
```
Expected: Tests pass (or skip if secrets aren't configured).

- [ ] **Step 3: Verify tests skip gracefully without secrets**

```bash
unset PESEPAY_SANDBOX_INTEGRATION_KEY
unset PESEPAY_SANDBOX_ENCRYPTION_KEY
dotnet test PesePay.Tests.Integration/PesePay.Tests.Integration.csproj -c Release
```
Expected: All tests skip with exit code 0. No `NullReferenceException`. `IAsyncLifetime` does not crash.

- [ ] **Step 4: Commit**

```bash
git add PesePay.Tests.Integration/PesePaySandboxTests.cs
git commit -m "fix: guard IAsyncLifetime.InitializeAsync against missing sandbox credentials"
```

---

### Task 5: Fix README installation command

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Fix package name**

On line 8, change:
```
dotnet add package PesePay
```
to:
```
dotnet add package Stelele.PesePay
```

The NuGet PackageId is `Stelele.PesePay`, not `PesePay`. Consumers using the old command would get a "package not found" error.

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: fix install command to use correct PackageId Stelele.PesePay"
```

---

### Task 6: Create `.github/workflows/ci.yml`

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create the workflow directory**

```bash
mkdir -p .github/workflows
```

- [ ] **Step 2: Write the workflow file**

```yaml
name: CI

on:
  push:
  pull_request:
  workflow_dispatch:

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  ci:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Restore
        run: dotnet restore PesePay.sln

      - name: Build
        run: dotnet build PesePay.sln -c Release --no-restore

      - name: Unit Tests
        run: dotnet test PesePay.Tests/PesePay.Tests.csproj -c Release --no-build --logger "console;verbosity=detailed"

      - name: Integration Tests
        if: github.event_name == 'push' || github.event.pull_request.head.repo.full_name == github.repository
        timeout-minutes: 5
        env:
          PESEPAY_SANDBOX_INTEGRATION_KEY: ${{ secrets.PESEPAY_SANDBOX_INTEGRATION_KEY }}
          PESEPAY_SANDBOX_ENCRYPTION_KEY: ${{ secrets.PESEPAY_SANDBOX_ENCRYPTION_KEY }}
          PESEPAY_SANDBOX_RESULT_URL: ${{ secrets.PESEPAY_SANDBOX_RESULT_URL }}
          PESEPAY_SANDBOX_RETURN_URL: ${{ secrets.PESEPAY_SANDBOX_RETURN_URL }}
        run: |
          if [ -z "$PESEPAY_SANDBOX_INTEGRATION_KEY" ] || [ -z "$PESEPAY_SANDBOX_ENCRYPTION_KEY" ]; then
            if [[ "${{ github.ref }}" == refs/tags/* ]] && [[ "${{ github.ref }}" != *-* ]]; then
              echo "::error::Sandbox secrets required for release tags. Configure PESEPAY_SANDBOX_INTEGRATION_KEY and PESEPAY_SANDBOX_ENCRYPTION_KEY."
              exit 1
            fi
            echo "::warning::Sandbox secrets not configured — skipping integration tests."
            exit 0
          fi
          dotnet test PesePay.Tests.Integration/PesePay.Tests.Integration.csproj -c Release --no-build --logger "console;verbosity=detailed"

      - name: Pack
        if: startsWith(github.ref, 'refs/tags/v') && !contains(github.ref, '-')
        run: dotnet pack PesePay.csproj -c Release --no-build -o ./nupkg

      - name: Validate version
        if: startsWith(github.ref, 'refs/tags/v') && !contains(github.ref, '-')
        run: |
          TAG_VERSION="${GITHUB_REF#refs/tags/v}"
          EXPECTED="Stelele.PesePay.${TAG_VERSION}.nupkg"
          ls "./nupkg/$EXPECTED" || (echo "::error::Version mismatch: expected $EXPECTED" && exit 1)

      - name: Push to NuGet
        if: startsWith(github.ref, 'refs/tags/v') && !contains(github.ref, '-')
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          dotnet nuget push ./nupkg/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
          dotnet nuget push ./nupkg/*.snupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add build, test, and publish to NuGet workflow"
```

---

### Task 7: Full verification

- [ ] **Step 1: Run the full build and test suite locally**

```bash
dotnet restore PesePay.sln
dotnet build PesePay.sln -c Release --no-restore
dotnet test PesePay.Tests/PesePay.Tests.csproj -c Release --no-build
```

Expected: Build succeeds. All unit tests pass.

- [ ] **Step 2: Verify integrations tests skip gracefully without secrets**

```bash
bash -c 'unset PESEPAY_SANDBOX_INTEGRATION_KEY PESEPAY_SANDBOX_ENCRYPTION_KEY PESEPAY_SANDBOX_RESULT_URL PESEPAY_SANDBOX_RETURN_URL && dotnet test PesePay.Tests.Integration/PesePay.Tests.Integration.csproj -c Release --no-build'
```

Expected: All tests skip, exit code 0.

- [ ] **Step 3: Verify pack produces the right files**

```bash
rm -rf nupkg
dotnet pack PesePay.csproj -c Release --no-build -o ./nupkg
ls nupkg/
```

Expected: Lists `Stelele.PesePay.<version>.nupkg` and `Stelele.PesePay.<version>.snupkg`.

- [ ] **Step 4: Push all changes to GitHub**

Push the branch. Check that the workflow triggers on GitHub Actions and the build/test steps pass.

---

### Post-implementation: Configure secrets

After the code is merged to `main`, configure these secrets in the GitHub repo (Settings → Secrets and variables → Actions):

| Secret | How to obtain |
|--------|---------------|
| `NUGET_API_KEY` | Generate at nuget.org → Account → API Keys |
| `PESEPAY_SANDBOX_INTEGRATION_KEY` | From PesePay sandbox dashboard |
| `PESEPAY_SANDBOX_ENCRYPTION_KEY` | From PesePay sandbox dashboard |
| `PESEPAY_SANDBOX_RESULT_URL` | A webhook.site or similar URL for test callbacks |
| `PESEPAY_SANDBOX_RETURN_URL` | Same as above |

Then publish a release:
```bash
git tag v1.0.0
git push origin v1.0.0
```
