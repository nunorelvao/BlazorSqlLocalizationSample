namespace BlazorSqlLocalizationSample.Localization;

public interface ILocalizationStore
{
    string GetString(string key);

    Task ReloadAsync(string culture);
}