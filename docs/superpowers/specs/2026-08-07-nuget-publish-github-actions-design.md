# NuGet Publish via GitHub Actions

## Overview

Add a GitHub Actions workflow that builds and tests the library on every push/PR, and automatically publishes to nuget.org when a non-prerelease Git tag is pushed.

## Decisions

| Decision | Choice |
|----------|--------|
| Trigger for publish | Non-prerelease Git tags (`v1.0.0` publishes; `v1.0.0-alpha.1` does not) |
| Destination | nuget.org (public) |
| Versioning | MinVer (already integrated) |
| Test gates | Full test suite (unit + integration) before every publish |
| Workflow structure | Single job; publish is a conditional step at the end |

## Files

### `PesePay.sln` (new)

Standard .NET solution file linking all projects. Required so `dotnet build` at repo root compiles the library and both test projects in one pass.

```
dotnet new sln -n PesePay
dotnet sln PesePay.sln add PesePay.csproj
dotnet sln PesePay.sln add PesePay.Tests/PesePay.Tests.csproj
dotnet sln PesePay.sln add PesePay.Tests.Integration/PesePay.Tests.Integration.csproj
```

### `.github/workflows/ci.yml` (new)

**Triggers:** `push` + `pull_request` + `workflow_dispatch`

**Concurrency:** group `${{ github.workflow }}-${{ github.ref }}`, cancel in-progress.

**Permissions:** `contents: read`, `id-token: write`

**Job: `ci`** — single job to avoid artifact-passing complexity between runners.

1. **Checkout** — `actions/checkout` with `fetch-depth: 0` (full history required by MinVer)

2. **Setup .NET** — `actions/setup-dotnet` using `global.json`

3. **Restore** — `dotnet restore PesePay.sln`

4. **Build** — `dotnet build PesePay.sln -c Release --no-restore`

5. **Unit tests** — `dotnet test PesePay.Tests/PesePay.Tests.csproj -c Release --no-build --logger "console;verbosity=detailed"`

6. **Integration tests** — `dotnet test PesePay.Tests.Integration/PesePay.Tests.Integration.csproj -c Release --no-build --logger "console;verbosity=detailed"`
   - **Environment vars:** `PESEPAY_SANDBOX_INTEGRATION_KEY`, `PESEPAY_SANDBOX_ENCRYPTION_KEY`, `PESEPAY_SANDBOX_RESULT_URL`, `PESEPAY_SANDBOX_RETURN_URL` from secrets
   - **Pre-step guard (bash):**
     ```bash
     if [ -z "$PESEPAY_SANDBOX_INTEGRATION_KEY" ] || [ -z "$PESEPAY_SANDBOX_ENCRYPTION_KEY" ]; then
       if [[ "${{ github.ref }}" == refs/tags/* ]] && [[ "${{ github.ref }}" != *-* ]]; then
         echo "::error::Sandbox secrets required for release tags. Configure PESEPAY_SANDBOX_INTEGRATION_KEY and PESEPAY_SANDBOX_ENCRYPTION_KEY."
         exit 1
       fi
       echo "::warning::Sandbox secrets not configured — skipping integration tests."
       exit 0
     fi
     ```
     Non-tag pushes skip gracefully. Non-prerelease tag pushes without secrets fail hard.
   - **Fork conditional:** `if: github.event_name == 'push' || github.event.pull_request.head.repo.full_name == github.repository` — fork PRs skip the step entirely (they can't access secrets)
   - **Timeout:** 5 minutes

7. **Pack** — `dotnet pack PesePay.csproj -c Release --no-build -o ./nupkg`
   - **Condition:** `startsWith(github.ref, 'refs/tags/v') && !contains(github.ref, '-')`

8. **Validate version** — assert `.nupkg` filename matches the Git tag:
   ```bash
   TAG_VERSION="${GITHUB_REF#refs/tags/v}"
   EXPECTED="Stelele.PesePay.${TAG_VERSION}.nupkg"
   ls "./nupkg/$EXPECTED" || (echo "::error::Version mismatch: expected $EXPECTED" && exit 1)
   ```
   - **Condition:** same as pack

9. **Push to NuGet** — `dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate` followed by `dotnet nuget push ./nupkg/*.snupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate`
   - **Condition:** same as pack

### `global.json` (new)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### `PesePay.csproj` (modified)

- Remove `<Version>1.0.0</Version>` — MinVer handles versioning from Git tags
- Add `<MinVerTagPrefix>v</MinVerTagPrefix>`
- Add `<IncludeSymbols>true</IncludeSymbols>` and `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`
- Add `<PackageReadmeFile>README.md</PackageReadmeFile>`
- Add `<PackageTags>pesepay;payment;gateway;zimbabwe;ecocash;visa;mastercard;zimswitch;paygo;omari;innbucks;mobile-money</PackageTags>`
- Add `<RepositoryType>git</RepositoryType>`

Also add an `<ItemGroup>` to include the README in the package:
```xml
<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="" />
</ItemGroup>
```

### `PesePay.Tests.Integration/SandboxSeamlessPaymentTests.cs` (modified)

Fix `IAsyncLifetime.InitializeAsync` to not crash when sandbox secrets are missing:

```csharp
public async Task InitializeAsync()
{
    if (string.IsNullOrEmpty(SandboxCredentials.IntegrationKey) ||
        string.IsNullOrEmpty(SandboxCredentials.EncryptionKey))
        return;  // credentials missing — tests will be skipped by [SandboxFact]

    _client = SandboxCredentials.CreateClientWithUrls();

    var usdResult = await _client.GetPaymentMethodsAsync("USD");
    if (usdResult.IsSuccess) _usdMethods = usdResult.Data!;

    var zigResult = await _client.GetPaymentMethodsAsync("ZiG");
    if (zigResult.IsSuccess) _zigMethods = zigResult.Data!;
}
```

### `README.md` (modified)

- Fix installation command: `dotnet add package PesePay` → `dotnet add package Stelele.PesePay`

## Authentication: Trusted Publishing (OIDC)

Instead of a long-lived API key, the workflow uses **NuGet Trusted Publishing** — GitHub OIDC exchanges a short-lived job token for a temporary (~1 hour) NuGet API key. No secrets to store, rotate, or leak.

**One-time setup on nuget.org:**
1. Sign in to nuget.org → user menu → **Trusted Publishing**
2. Create a policy: owner = your account, repository = `Stelele/pesepay`, workflow file = `.github/workflows/ci.yml`

## Secrets & Variables

| Name | Type | Purpose |
|------|------|---------|
| `NUGET_USER` | Variable | Your nuget.org username (profile name, not email) — passed to `NuGet/login@v1` |
| `PESEPAY_SANDBOX_INTEGRATION_KEY` | Secret | Sandbox integration key |
| `PESEPAY_SANDBOX_ENCRYPTION_KEY` | Secret | Sandbox encryption key |
| `PESEPAY_SANDBOX_RESULT_URL` | Secret | Webhook result URL |
| `PESEPAY_SANDBOX_RETURN_URL` | Secret | Webhook return URL |

## Publishing flow

1. One-time: Create Trusted Publishing policy on nuget.org linking `Stelele/pesepay` + `ci.yml`
2. Push a non-prerelease tag: `git tag v1.0.0 && git push origin v1.0.0`
3. CI workflow runs: build → unit tests → integration tests (with credentials, hard-fails if missing on release tags)
4. All pass → pack → validate version → NuGet OIDC login → push to nuget.org
5. Package `Stelele.PesePay` (with `.snupkg` symbols) available on nuget.org

## Error handling

- Test failures stop the job immediately; publish steps never run
- Missing sandbox secrets on non-tag pushes: warning + skip integration tests
- Missing sandbox secrets on release tag pushes: hard failure, blocks publish
- NuGet push with duplicate version: `--skip-duplicate` handles re-runs
- NuGet push for other reasons: workflow fails
- Version mismatch between tag and MinVer output: validate step catches and fails
- `IAsyncLifetime` crash when secrets missing: guarded in `InitializeAsync`

## Known limitations (not addressed)

- Test projects target only `net10.0` while library targets `net8.0;net9.0;net10.0`
- No code coverage threshold enforced
- No GitHub Environment protection on publish (requires paid plan)
- No NuGet package signing (requires certificate infrastructure)
