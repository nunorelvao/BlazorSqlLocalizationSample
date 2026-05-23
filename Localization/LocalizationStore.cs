using BlazorSqlLocalizationSample.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace BlazorSqlLocalizationSample.Localization;

public class LocalizationStore : ILocalizationStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocalizationStore> _logger;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    public LocalizationStore(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    // New constructor overload for DI to provide ILogger
    public LocalizationStore(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<LocalizationStore> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public string GetString(string key)
    {
        var culture = CultureInfo.CurrentUICulture.Name;

        var resources = GetCultureDictionary(culture);

        return resources.TryGetValue(key, out var value)
            ? value
            : $"[[{key}]]";
    }

    private Dictionary<string, string> GetCultureDictionary(
        string culture)
    {
        var cacheKey = $"LOC_{culture}";

        if (_cache.TryGetValue(
            cacheKey,
            out Dictionary<string, string>? dict))
        {
            return dict!;
        }

        _lock.Wait();

        try
        {
            if (_cache.TryGetValue(cacheKey, out dict))
                return dict!;

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<LocalizationDbContext>();

            dict = db.LocalizationResources
                .AsNoTracking()
                .Where(x => x.Culture == culture)
                .ToDictionary(
                    x => x.ResourceKey,
                    x => x.ResourceValue);


            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
                SlidingExpiration = TimeSpan.FromHours(1),
                Priority = CacheItemPriority.High,
                Size = 1
            };

            cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                try
                {
                    _logger?.LogInformation("Cache entry {Key} evicted due to {Reason}", key, reason);
                    // Optionally kick off a background reload to warm cache
                    _ = ReloadAsync(state as string ?? culture);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during eviction callback for {Key}", key);
                }
            }, state: culture);

            _cache.Set(cacheKey, dict, cacheOptions);

            return dict;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task ReloadAsync(string culture)
    {
        _cache.Remove($"LOC_{culture}");

        return Task.CompletedTask;
    }

    public void EnsureLoaded(string culture)
    {
        // Force loading into cache if not present
        GetCultureDictionary(culture);
    }
}