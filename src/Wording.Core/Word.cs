using Wording.Core.Learning;

namespace Wording.Core;

public sealed class Word
{
    /// <summary>
    /// GUID, a nie licznik - dwa urzadzenia dodajace slowko offline nie moga
    /// wygenerowac tego samego identyfikatora. Stary format (Id = max + 1)
    /// przetwarzal identyfikatory po skasowaniu ostatniego wpisu.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Original { get; set; } = string.Empty;

    public string Translation { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Stan powtorek. Wedruje razem ze slowkiem, zeby nauka byla spojna miedzy urzadzeniami.</summary>
    public ReviewState Review { get; set; } = new();
}
