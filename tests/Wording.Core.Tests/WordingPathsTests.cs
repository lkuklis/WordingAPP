using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordingPathsTests
{
    [Fact]
    public void DataDirectory_IsAnAbsolutePath()
    {
        // The point of the fix: the old version read "WordsData.xml" relative to the
        // process working directory, so launching from elsewhere crashed the app.
        Assert.True(Path.IsPathRooted(WordingPaths.DataDirectory()));
        Assert.True(Path.IsPathRooted(WordingPaths.DataFile()));
    }

    [Fact]
    public void DataFile_LivesInTheApplicationDirectory()
    {
        var file = WordingPaths.DataFile();

        Assert.Equal(WordingPaths.DataFileName, Path.GetFileName(file));
        Assert.Equal(WordingPaths.AppFolderName, Path.GetFileName(Path.GetDirectoryName(file)));
    }

    [Fact]
    public void OnMacOs_UsesLibraryApplicationSupport()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        Assert.Contains(Path.Combine("Library", "Application Support"), WordingPaths.DataDirectory());
    }
}
