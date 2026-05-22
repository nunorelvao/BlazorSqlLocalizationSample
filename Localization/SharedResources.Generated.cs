namespace BlazorSqlLocalizationSample.Localization;

public static partial class SharedResources
{
    public static string Welcome =>
        LocalizationProvider.GetString(nameof(Welcome));

    public static string Save =>
        LocalizationProvider.GetString(nameof(Save));

    public static string Logout =>
        LocalizationProvider.GetString(nameof(Logout));

    public static string Login =>
        LocalizationProvider.GetString(nameof(Login));

    public static string Dashboard =>
        LocalizationProvider.GetString(nameof(Dashboard));
}