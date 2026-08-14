using System.Xml.Linq;
using Wording.Core.Learning;

namespace Wording.Core.Storage;

/// <summary>
/// Czyta stary format &lt;AllWords&gt;/&lt;Word&gt;/&lt;Id|Original|Translated&gt;.
/// Tylko do odczytu - sluzy wylacznie jednorazowej migracji na JSON.
/// </summary>
public static class LegacyXmlImporter
{
    public static IReadOnlyList<Word> Read(string xmlPath, DateTimeOffset now)
    {
        var dokument = XDocument.Load(xmlPath);

        return dokument.Descendants("Word")
            .Select(element => new Word
            {
                // Stare liczbowe Id celowo odrzucamy - byly przetwarzane po kasowaniu
                // wpisow, wiec nie nadaja sie na trwaly identyfikator.
                Id = Guid.NewGuid(),
                Original = (string?)element.Element("Original") ?? string.Empty,
                Translation = (string?)element.Element("Translated") ?? string.Empty,
                CreatedUtc = now,
                Review = ReviewState.New(now),
            })
            .Where(word => word.Original.Length > 0)
            .ToList();
    }
}
