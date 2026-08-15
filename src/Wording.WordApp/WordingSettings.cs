using Microsoft.Extensions.Configuration;
using Wording.Core.Storage;

namespace Wording.WordApp;

/// <summary>
/// Ustawienia aplikacji z appsettings.json. Konfiguracje czyta wylacznie warstwa
/// aplikacji - Wording.Core dostaje gotowe wartosci.
/// </summary>
public sealed class WordingSettings
{
    public const string SectionName = "wording";

    /// <summary>Co ile sekund pokazac kolejne slowko.</summary>
    public int ChangeTimeSeconds { get; set; } = 5;

    /// <summary>Jak dlugo powiadomienie ma byc widoczne.</summary>
    public int ShowTimeSeconds { get; set; } = 6;

    /// <summary>Nadpisanie sciezki pliku danych. Puste - katalog danych uzytkownika.</summary>
    public string? DataFile { get; set; }

    public static WordingSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var settings = new WordingSettings();
        configuration.GetSection(SectionName).Bind(settings);

        // Chroni przed zerowym albo ujemnym interwalem w recznie edytowanym pliku.
        settings.ChangeTimeSeconds = Math.Max(1, settings.ChangeTimeSeconds);
        settings.ShowTimeSeconds = Math.Max(1, settings.ShowTimeSeconds);

        return settings;
    }

    /// <summary>Docelowa sciezka pliku ze slowkami.</summary>
    public string ResolveDataFile() =>
        string.IsNullOrWhiteSpace(DataFile) ? WordingPaths.DataFile() : DataFile;

    /// <summary>
    /// Plik w starym formacie XML, o ile istnieje. Szukamy obok pliku exe (tam trafia
    /// pakiet startowy) oraz w katalogu roboczym, gdzie trzymala go stara wersja.
    /// </summary>
    public static string? FindLegacyXml()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, WordingPaths.LegacyDataFileName),
            Path.Combine(Directory.GetCurrentDirectory(), WordingPaths.LegacyDataFileName),
        ];

        return Array.Find(candidates, File.Exists);
    }
}
