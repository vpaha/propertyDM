using System.Globalization;

public sealed record CultureOption(string Country, string Culture);

public sealed class CultureData
{
    public CultureOption[] Options { get; } =
    [
        new("United States", "en-US"),
        new("United Kingdom", "en-GB"),
        new("Germany", "de-DE"),
        new("France", "fr-FR"),
        new("Spain", "es-ES"),
        new("Brazil", "pt-BR"),
        new("Russia", "ru-RU"),
        new("China", "zh-CN"),
        new("Japan", "ja-JP")
    ];

    public List<CultureInfo> CultureList =>
       Options.Select(c => new CultureInfo(c.Culture)).ToList();

    public CultureInfo DefaultCulture { get; } = new("en-US");
}