# BlazorSqlLocalizationSample

Lightweight Blazor Razor Components sample that loads localization resources from a database (SQLite or SQL Server).

Prerequisites
- .NET 10 SDK
- Visual Studio 2026 (recommended) or dotnet CLI

Quick start
1. Open the solution in Visual Studio or run from the repo root:

   dotnet build
   dotnet run

2. Open the site at the URL shown in the console (https://localhost:<port>). The app will attempt to use the SQLite connection string first if configured.

Configuration
- appsettings.json contains two sample connection strings:
  - `SQLLite` — e.g. `Data Source=test.db` (used first if present)
  - `SQLServer` — used if no SQLite connection string is supplied
- The application will create the SQLite DB file and run `SQLScripts.sql` at startup when `SQLLite` is configured. The SQL script is idempotent so it can run multiple times safely.

How localization data works
- The sample uses an EF Core DbContext (Data/LocalizationDbContext) and a small LocalizationStore that caches values and serves them via LocalizationProvider.
- SharedResources.* exposes properties that call into LocalizationProvider.Get(name).

Diagnostics
- The app exposes lightweight diagnostic endpoints (useful during development):
  - `/_diagnostics/ping` — returns pong
  - `/_diagnostics/endpoints` — JSON list of registered endpoints
- Startup logging also prints discovered routed components (eg. Home -> `/`). Check the Visual Studio Output window.

Interactive runtime vs server-rendering
- By default the project is configured to host Razor Components. The codebase contains support for both interactive server mode and a server-side fallback.
- If you want a fully interactive experience (SignalR + blazor.server.js) make sure the browser loads `_framework/blazor.server.js` and that `/ _blazor/negotiate` is reachable. If those do not appear in the browser network tab, the interactive runtime won't initialize and pages may remain blank.

Troubleshooting blank pages
- Check browser DevTools (Console and Network) for requests to:
  - `/_framework/blazor.server.js` (should return 200)
  - `/_blazor/negotiate` (should return JSON)
- Ensure static files are served (no stale `wwwroot/index.html` that conflicts with component routing). If you want components to own `/`, do not keep a static `index.html` under wwwroot.
- Use the diagnostic endpoints above to list registered endpoints.
- If you see antiforgery-related exceptions, confirm `builder.Services.AddAntiforgery()` and `app.UseAntiforgery()` are present (the sample adds these when required).

Development notes
- The SQLite script `SQLScripts.sql` is located in the repo and is copied to the output so it runs at startup for SQLite.
- To switch to SQL Server, set `ConnectionStrings:SQLServer` in appsettings.json and remove/clear `SQLLite`.

If you want me to trim the server-side fallback or re-enable interactive mode and help diagnose the negotiate/script loading, tell me which mode you prefer and I will adjust the project accordingly.

Test project purpose
- This repository is a test project created to demonstrate how an existing application that uses .resx resource files can be migrated to load localization strings from a database.
- It is intended as a proof-of-concept and includes the minimal plumbing to:
  1. Extract resource keys used across the application (represented by SharedResources.* in this sample).
  2. Store resources in a table (LocalizationResources) keyed by ResourceKey and Culture.
  3. Provide a lightweight ILocalizationStore implementation that reads from the DB and caches results for performance.
  4. Offer a simple provider (LocalizationProvider) that client code can call to get localized strings.

Migration considerations (high level)
- Inventory resource usage: enumerate all keys used in .resx and replace direct resource lookups with calls to the provider.
- Seed data: migrate existing .resx values into the LocalizationResources table (SQLScripts.sql shows an example seed).
- Caching: database lookups can be cached in memory with sliding/absolute expirations; this sample uses IMemoryCache.
- Concurrency & updates: provide a ReloadAsync(culture) mechanism so administrative UIs can refresh cache after edits.
- Formats & culture fallback: implement culture fallback rules (e.g., en-US -> en) as needed by the app.
- Tooling: for large projects, consider a script that converts .resx files into INSERT statements or uses a migration step to populate the DB.

Security & operations
- Secure access to the database and limit who can edit localization entries.
- Consider adding versioning/audit columns to LocalizationResources for change tracking.
- If using SQL Server, adapt SQLScripts.sql to be compatible with T-SQL and use proper idempotent checks (IF NOT EXISTS ...).

Resx-to-SQL conversion tool
- Location: tools/resx-to-sql.ps1
- Purpose: converts .resx files into idempotent SQL INSERT statements suitable for seeding the LocalizationResources table.
- Supported providers: sqlite (default) and sqlserver (T-SQL formatting).
- Parameters:
  -InputDir (default: .) : directory to scan for .resx files (recurses).
  -OutputFile (default: resx-inserts.sql) : path to write SQL statements.
  -DefaultCulture (default: en) : culture to assign to neutral .resx files (e.g., Resources.resx).
  -Provider (sqlite|sqlserver) : controls SQL dialect/emitted statements.
- Example:
  powershell.exe -ExecutionPolicy Bypass -File .\tools\resx-to-sql.ps1 -InputDir .\Resources -OutputFile seed.sql -Provider sqlite
- Notes:
  - The script emits idempotent INSERTs using a WHERE NOT EXISTS check so the statements can be executed repeatedly without creating duplicates.
  - For large projects, run the script as part of a localization migration step and review the generated SQL before applying to production.

SQLScripts.sql details
- The file SQLScripts.sql in the repository is written for SQLite and uses CREATE TABLE IF NOT EXISTS and INSERT ... SELECT ... WHERE NOT EXISTS so it is safe to run multiple times.
- If you switch to SQL Server, replace this file with an idempotent T-SQL script (IF NOT EXISTS ... INSERT ...).

Application modes and how to control them
- Interactive server components (SignalR + blazor.server.js): when enabled the browser loads _framework/blazor.server.js and establishes a SignalR connection to the server. This gives full interactivity but requires the negotiate /_blazor endpoints and websockets to work.
- Server-render fallback: the project includes a safe server-side fallback that directly renders Home/Test components on the server so the initial view appears even if the interactive runtime fails to initialize.
- To prefer interactive mode: ensure MapRazorComponents<App>().AddInteractiveServerRenderMode() is configured and that the client can load _framework/blazor.server.js. If you prefer server rendering only, MapRazorComponents<App>() without interactive bindings is sufficient.

Diagnostics and troubleshooting checklist
- Endpoints:
  - /_diagnostics/ping returns pong.
  - /_diagnostics/endpoints returns a JSON array of registered endpoints and patterns.
- When pages are blank, check these in order:
  1. Browser DevTools -> Network: _framework/blazor.server.js should be requested and return 200.
  2. Browser DevTools -> Network: /_blazor/negotiate should be requested and return JSON.
  3. Server logs (Visual Studio Output) show routed components discovered (Found routed component: ...).
  4. Ensure no static wwwroot/index.html is interfering if you want components to own the root path.
  5. If you see antiforgery errors, confirm AddAntiforgery + UseAntiforgery are present in Program.cs.

CI / automation
- You can run the resx-to-sql.ps1 script as part of a build or migration pipeline to produce a seed SQL file. Review and apply the generated SQL during deployment.

Where to look in this repo
- Program.cs - application startup, DB initialization, diagnostics wiring, and component hosting mode.
- SQLScripts.sql - idempotent SQLite seed script.
- Data/LocalizationDbContext.cs and Data/LocalizationResource.cs - EF Core model.
- Localization/LocalizationStore.cs and Localization/ILocalizationStore.cs - runtime store + cache.
- Localization/SharedResources.* - generated helper for localization property access.
- tools/resx-to-sql.ps1 - resx -> SQL converter script.

Memory cache configuration
--------------------------
The app binds IMemoryCache options from the MemoryCache section in appsettings.json. The following settings are available:

- SizeLimit (long) — enables a size-based eviction policy. Entries must set a Size value via MemoryCacheEntryOptions.Size to be counted against this limit.
- CompactionPercentage (double 0..1) — when the cache exceeds SizeLimit, this fraction of the cache will be compacted on the next pass.
- ExpirationScanFrequency (TimeSpan) — how often the cache scans for expired entries and performs maintenance.

Example appsettings.json (already present in this repo):

```json
"MemoryCache": {
  "SizeLimit": 1024,
  "CompactionPercentage": 0.2,
  "ExpirationScanFrequency": "00:05:00"
}
```

Per-entry options (recommended)
- Use MemoryCacheEntryOptions to control lifetimes and eviction behavior per entry. Typical options:
  - AbsoluteExpirationRelativeToNow: a hard TTL
  - SlidingExpiration: resets on access
  - Priority: CacheItemPriority.High / Normal / Low / NeverRemove
  - Size: long (counts against SizeLimit)
  - RegisterPostEvictionCallback: useful to trigger a refresh or log evictions

Example usage in LocalizationStore (pseudo-code):

```csharp
var cacheOptions = new MemoryCacheEntryOptions
{
	AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
	SlidingExpiration = TimeSpan.FromHours(1),
	Priority = CacheItemPriority.High,
	Size = 1
};

cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
{
	// Optionally refresh or log
	_ = ReloadAsync(culture: (string?)state);
});

_cache.Set(cacheKey, dict, cacheOptions);
```

Notes & recommendations
- If you enable SizeLimit, ensure you set Size for all entries (choose a unit such as 1 per culture dictionary).
- Use sliding expiration for frequently-accessed culture dictionaries and absolute expiration for guaranteed periodic refresh.
- Use the eviction callback to trigger asynchronous reloads if you want the cache to be warmed on eviction.
- For multi-node deployments prefer an IDistributedCache (Redis) to share cache state across instances.
