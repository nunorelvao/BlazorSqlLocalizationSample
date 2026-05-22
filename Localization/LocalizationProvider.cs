namespace BlazorSqlLocalizationSample.Localization;

public static class LocalizationProvider
{
    public static ILocalizationStore Store { get; set; } = null!;

    public static string GetString(string key)
    {
        return Store.GetString(key);
    }
}