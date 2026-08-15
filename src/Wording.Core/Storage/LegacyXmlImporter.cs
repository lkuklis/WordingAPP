using System.Xml.Linq;
using Wording.Core.Learning;

namespace Wording.Core.Storage;

/// <summary>
/// Reads the legacy &lt;AllWords&gt;/&lt;Word&gt;/&lt;Id|Original|Translated&gt; format.
/// Read-only: it exists purely for the one-off migration to JSON.
/// </summary>
public static class LegacyXmlImporter
{
    public static IReadOnlyList<Word> Read(string xmlPath, DateTimeOffset now)
    {
        var document = XDocument.Load(xmlPath);

        return document.Descendants("Word")
            .Select(element => new Word
            {
                // The old numeric ids are dropped on purpose: they were recycled when
                // entries were deleted, so they are unusable as stable identifiers.
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
