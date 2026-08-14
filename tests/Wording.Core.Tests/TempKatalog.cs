using System.Xml.Linq;

namespace Wording.Core.Tests;

/// <summary>
/// Izolowany katalog na dane, sprzatany po tescie.
/// </summary>
sealed class TempKatalog : IDisposable
{
    public string Sciezka { get; }

    public TempKatalog()
    {
        Sciezka = Path.Combine(Path.GetTempPath(), "wording-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(Sciezka);
    }

    public string PlikJson => Path.Combine(Sciezka, "words.json");

    public string PlikXml => Path.Combine(Sciezka, "WordsData.xml");

    /// <summary>Tworzy plik w starym formacie XML, uzywany w testach migracji.</summary>
    public string ZapiszStaryXml(params (int Id, string Original, string Translated)[] slowa)
    {
        new XDocument(
            new XElement("AllWords",
                slowa.Select(s => new XElement("Word",
                    new XElement("Id", s.Id),
                    new XElement("Original", s.Original),
                    new XElement("Translated", s.Translated)))))
            .Save(PlikXml);

        return PlikXml;
    }

    public void Dispose()
    {
        if (Directory.Exists(Sciezka))
        {
            Directory.Delete(Sciezka, recursive: true);
        }
    }
}
