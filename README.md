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
