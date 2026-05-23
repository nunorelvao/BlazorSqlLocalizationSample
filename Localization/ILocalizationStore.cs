namespace BlazorSqlLocalizationSample.Localization;

public interface ILocalizationStore
{
    string GetString(string key);

    Task ReloadAsync(string culture);

    // Ensure the cache for a specific culture is loaded (warm-up)
    void EnsureLoaded(string culture);
}