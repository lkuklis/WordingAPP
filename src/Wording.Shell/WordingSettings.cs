using Microsoft.Extensions.Configuration;
using Wording.Core.Storage;

namespace Wording.Shell;

/// <summary>
/// Ustawienia aplikacji z appsettings.json.
/// <para>
/// Wczesniej siedzialy w App.config i czytal je bezposrednio Wording.Core, przez co
/// biblioteka logiki byla zwiazana z konfiguracja procesu. Teraz konfiguracje czyta
/// wylacznie warstwa aplikacji i przekazuje gotowe wartosci w dol.
/// </para>
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
        var konfiguracja = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var ustawienia = new WordingSettings();
        konfiguracja.GetSection(SectionName).Bind(ustawienia);

        return ustawienia.Sanitized();
    }

    /// <summary>Docelowa sciezka pliku ze slowkami.</summary>
    public string ResolveDataFile() =>
        string.IsNullOrWhiteSpace(DataFile) ? WordingPaths.DataFile() : DataFile;

    /// <summary>
    /// Plik w starym formacie XML, o ile w ogole istnieje. Szukamy obok pliku exe
    /// (tam trafia pakiet startowy) oraz w katalogu roboczym, gdzie trzymala go
    /// poprzednia wersja aplikacji.
    /// </summary>
    public static string? FindLegacyXml()
    {
        string[] kandydaci =
        [
            Path.Combine(AppContext.BaseDirectory, WordingPaths.LegacyDataFileName),
            Path.Combine(Directory.GetCurrentDirectory(), WordingPaths.LegacyDataFileName),
        ];

        return Array.Find(kandydaci, File.Exists);
    }

    /// <summary>Chroni przed zerowym albo ujemnym interwalem w recznie edytowanym pliku.</summary>
    WordingSettings Sanitized()
    {
        ChangeTimeSeconds = Math.Max(1, ChangeTimeSeconds);
        ShowTimeSeconds = Math.Max(1, ShowTimeSeconds);

        return this;
    }
}
