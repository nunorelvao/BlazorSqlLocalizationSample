namespace BlazorSqlLocalizationSample.Data;

public class LocalizationResource
{
    public int Id { get; set; }

    public string ResourceKey { get; set; } = null!;

    public string Culture { get; set; } = null!;

    public string ResourceValue { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}