using Wording.Core.Packs;
using Wording.Core.Storage;

namespace Wording.WordApp;

/// <summary>
/// Remembers which set the user was learning from, in the data directory next to the
/// words themselves.
/// <para>
/// Not in appsettings.json: that is configuration shipped next to the executable and
/// read-only at runtime, while this is a choice the user makes in the app. The macOS
/// port keeps the same value in UserDefaults - the two ports diverge here because
/// WinForms has no equivalent, and the file is one line so it cannot grow a
/// serialization trap of its own.
/// </para>
/// </summary>
public sealed class WordingState
{
    public const string FileName = "active-set.txt";

    readonly string _path;

    public WordingState(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        _path = Path.Combine(dataDirectory, FileName);
        ActiveSetId = Read();
    }

    /// <summary>Null means the user's own words.</summary>
    public string? ActiveSetId { get; private set; }

    public void Remember(string? setId)
    {
        ActiveSetId = setId;

        try
        {
            if (setId is null)
            {
                File.Delete(_path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, setId);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Losing the choice on the next start is a nuisance; refusing to switch sets
            // because it could not be written down would be worse.
        }
    }

    string? Read()
    {
        try
        {
            return File.Exists(_path) && PackSlug.TryNormalize(File.ReadAllText(_path).Trim(), out var slug)
                ? slug
                : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The data directory this state file lives in.</summary>
    public static string DirectoryFor(WordingSettings settings) =>
        Path.GetDirectoryName(settings.ResolveDataFile()) ?? WordingPaths.DataDirectory();
}
