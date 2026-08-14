using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordingPathsTests
{
    [Fact]
    public void KatalogDanych_JestSciezkaBezwzgledna()
    {
        // Sedno naprawy: stara wersja czytala "WordsData.xml" wzgledem katalogu
        // roboczego procesu, wiec uruchomienie z innego miejsca wywalalo aplikacje.
        Assert.True(Path.IsPathRooted(WordingPaths.DataDirectory()));
        Assert.True(Path.IsPathRooted(WordingPaths.DataFile()));
    }

    [Fact]
    public void PlikDanych_LezyWKataloguAplikacji()
    {
        var plik = WordingPaths.DataFile();

        Assert.Equal(WordingPaths.DataFileName, Path.GetFileName(plik));
        Assert.Equal(WordingPaths.AppFolderName, Path.GetFileName(Path.GetDirectoryName(plik)));
    }

    [Fact]
    public void NaMacOs_UzywaLibraryApplicationSupport()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        Assert.Contains(Path.Combine("Library", "Application Support"), WordingPaths.DataDirectory());
    }
}
