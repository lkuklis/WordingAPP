namespace Wording.Core.Packs;

/// <summary>
/// Tidying shared by the pack reader and the catalogue reader. Both take text from a file
/// downloaded off the internet and put it in a notification and a list, so both need the
/// same two guarantees: nothing invisible, and nothing endless.
/// </summary>
static class Text
{
    /// <summary>
    /// Trims, and folds every control character - newlines and tabs included - into a
    /// space. They would otherwise reach a notification body and a grid cell.
    /// </summary>
    public static string Clean(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var characters = new char[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            characters[i] = char.IsControl(value[i]) ? ' ' : value[i];
        }

        return new string(characters).Trim();
    }

    public static string Truncate(string value, int limit) =>
        value.Length <= limit ? value : value[..limit].TrimEnd();
}
