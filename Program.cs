using BlazorSqlLocalizationSample.Data;
using BlazorSqlLocalizationSample.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using BlazorSqlLocalizationSample.Components;

var builder = WebApplication.CreateBuilder(args);


// Configure Razor Components for server-side rendering only (no interactive runtime)
builder.Services.AddRazorComponents();

// Configure IMemoryCache using settings from appsettings.json (MemoryCache section)
builder.Services.AddMemoryCache(options =>
{
    builder.Configuration.GetSection("MemoryCache").Bind(options);
});

// Antiforgery is required by Razor Components endpoints that include antiforgery metadata
builder.Services.AddAntiforgery();

var sqlLiteConn = builder.Configuration.GetConnectionString("SQLLite");
var sqlServerConn = builder.Configuration.GetConnectionString("SQLServer");

if (!string.IsNullOrEmpty(sqlLiteConn) &&
    sqlLiteConn.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<LocalizationDbContext>(options =>
        options.UseSqlite(sqlLiteConn));
}
else if (!string.IsNullOrEmpty(sqlServerConn))
{
    builder.Services.AddDbContext<LocalizationDbContext>(options =>
        options.UseSqlServer(sqlServerConn));
}
else
{
    throw new InvalidOperationException("No valid connection string found. Configure 'SQLServer' or 'SQLLite' connection string.");
}

builder.Services.AddSingleton<ILocalizationStore, LocalizationStore>();

var app = builder.Build();

// Create a logger for startup diagnostics
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StartupDiagnostics");
startupLogger.LogInformation("Application built. Beginning startup diagnostics...");

// Request/response logging middleware for detailed diagnostics
app.Use(async (context, next) =>
{
    try
    {
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;
        var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        startupLogger.LogInformation("Incoming request {Method} {Path}{Query} IsWebSocketRequest={IsWs}", method, path, qs, context.WebSockets.IsWebSocketRequest);
    }
    catch (Exception ex)
    {
        startupLogger.LogDebug(ex, "Error logging request");
    }

    await next();

    try
    {
        startupLogger.LogInformation("Responded {StatusCode} for {Path}", context.Response.StatusCode, context.Request.Path);
    }
    catch (Exception ex)
    {
        startupLogger.LogDebug(ex, "Error logging response");
    }
});

// Reflectively discover component route attributes to verify pages are compiled with route metadata
try
{
    var routeAttrType = typeof(Microsoft.AspNetCore.Components.RouteAttribute);
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    foreach (var asm in assemblies)
    {
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (Exception ex) when (ex.GetType().Name == "ReflectionTypeLoadException")
        {
            var rtlex = ex;
            var typesField = rtlex.GetType().GetField("Types");
            types = ((Type[]?)typesField?.GetValue(rtlex))?.Where(t => t != null).ToArray() ?? Array.Empty<Type>();
        }

        foreach (var t in types)
        {
            if (t == null) continue;

            try
            {
                var attrs = t.GetCustomAttributes(routeAttrType, inherit: false);
                foreach (var a in attrs)
                {
                    // RouteAttribute has a Template property
                    var templateProp = a.GetType().GetProperty("Template");
                    var template = templateProp?.GetValue(a) as string;
                    startupLogger.LogInformation("Found routed component: {Type} -> {Template}", t.FullName, template);
                }
            }
            catch (Exception ex)
            {
                // ignore type inspection errors
                startupLogger.LogDebug(ex, "Error inspecting type {Type}", t.FullName);
            }
        }
    }
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Error during reflective component route discovery.");
}

// If using SQLite, ensure the database file is created and run the initialization script.
if (!string.IsNullOrEmpty(sqlLiteConn) &&
    sqlLiteConn.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<LocalizationDbContext>();

        // Ensure database (file) and EF model are created
        db.Database.EnsureCreated();

        // Execute SQL script if present in output folder
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "SQLScripts.sql");
        if (File.Exists(scriptPath))
        {
            var script = File.ReadAllText(scriptPath);

            var conn = db.Database.GetDbConnection();
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = script;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                try { conn.Close(); } catch { }
            }
        }
    }

}

var supportedCultures = new[]
{
      new CultureInfo("pt"),
     //ew CultureInfo("en")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

LocalizationProvider.Store =
    app.Services.GetRequiredService<ILocalizationStore>();

// Warm-up default culture cache
try
{
    var defaultCulture = supportedCultures.First();

    CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
    CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

    startupLogger.LogInformation("Warming up localization cache for default culture: {Culture}", defaultCulture.Name);
    app.Services.GetRequiredService<ILocalizationStore>().EnsureLoaded(defaultCulture.Name);
}
catch (Exception ex)
{
    startupLogger.LogWarning(ex, "Failed to warm up localization cache at startup.");
}

// Serve static files first so default documents (index.html) are handled by static file middleware
app.UseStaticFiles();

app.UseRouting();

// Enable antiforgery middleware so endpoints with antiforgery metadata are handled correctly
app.UseAntiforgery();
// Blazor hub is provided by MapRazorComponents in this hosting mode; avoid mapping the hub twice.
// If you need to explicitly map the hub, uncomment the following line.
// app.MapBlazorHub();

startupLogger.LogInformation("Mapping Razor Components for App (server render only)...");
app.MapRazorComponents<App>();
startupLogger.LogInformation("MapRazorComponents called.");

// Log endpoint count after mapping to help confirm component endpoints registered
try
{
    var endpointSourcePost = app.Services.GetService<EndpointDataSource>();
    if (endpointSourcePost != null)
    {
        startupLogger.LogInformation("Endpoint count after mapping: {Count}", endpointSourcePost.Endpoints.Count);
    }
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Error while logging endpoint count after mapping.");
}

// Diagnostic endpoints: list endpoints and a simple ping
app.MapGet("/_diagnostics/ping", () => "pong");

app.MapGet("/_diagnostics/endpoints", (EndpointDataSource ds) =>
{
    var list = ds.Endpoints.Select(ep => new
    {
        DisplayName = ep.DisplayName,
        Pattern = (ep as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern?.RawText
    });

    return Results.Json(list);
});

// Reset localization cache endpoint
// Usage:
//   GET /resetlocalizationcache            -> clears cache for all supported cultures
//   GET /resetlocalizationcache?culture=pt -> clears cache for specified culture
app.MapGet("/resetlocalizationcache", async (ILocalizationStore store, string? culture) =>
{
    if (string.IsNullOrEmpty(culture))
    {
        foreach (var c in supportedCultures)
        {
            await store.ReloadAsync(c.Name);
        }

        return Results.Ok(new { cleared = supportedCultures.Select(c => c.Name).ToArray() });
    }

    await store.ReloadAsync(culture);
    return Results.Ok(new { cleared = new[] { culture } });
});

// Log endpoints to check that routing for components is configured
try
{
    var endpointSource = app.Services.GetService<EndpointDataSource>();
    if (endpointSource != null)
    {
        foreach (var ep in endpointSource.Endpoints)
        {
            startupLogger.LogInformation("Endpoint: {Endpoint}", ep.DisplayName ?? ep.ToString());
        }
    }
    else
    {
        startupLogger.LogWarning("EndpointDataSource not available from DI.");
    }
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Error while enumerating endpoints.");
}

app.Run();