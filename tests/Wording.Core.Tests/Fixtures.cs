using System.Xml.Linq;
using Microsoft.Extensions.Time.Testing;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

/// <summary>A single reference point for every test: one instant and one clock.</summary>
static class Fixtures
{
    public static readonly DateTimeOffset Now = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Clock() => new(Now);
}

/// <summary>An isolated data directory, removed after the test.</summary>
sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "wording-test-" + System.IO.Path.GetRandomFileName());

        Directory.CreateDirectory(Path);
    }

    public string JsonFile => System.IO.Path.Combine(Path, WordingPaths.DataFileName);

    public string SetsDirectory => System.IO.Path.Combine(Path, WordingPaths.SetsFolderName);

    public string SetFile(string slug) => WordingPaths.SetFile(slug, SetsDirectory);

    public string XmlFile => System.IO.Path.Combine(Path, WordingPaths.LegacyDataFileName);

    /// <summary>Writes a file in the legacy XML format, used by the import tests.</summary>
    public string WriteLegacyXml(params (int Id, string Original, string Translated)[] words)
    {
        new XDocument(
            new XElement("AllWords",
                words.Select(word => new XElement("Word",
                    new XElement("Id", word.Id),
                    new XElement("Original", word.Original),
                    new XElement("Translated", word.Translated)))))
            .Save(XmlFile);

        return XmlFile;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
