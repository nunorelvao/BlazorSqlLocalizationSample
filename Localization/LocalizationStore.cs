using BlazorSqlLocalizationSample.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace BlazorSqlLocalizationSample.Localization;

public class LocalizationStore : ILocalizationStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    private static readonly SemaphoreSlim _lock = new(1, 1);

    public LocalizationStore(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
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

            _cache.Set(
                cacheKey,
                dict,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(6),

                    SlidingExpiration =
                        TimeSpan.FromHours(1),

                    Priority = CacheItemPriority.High
                });

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
}