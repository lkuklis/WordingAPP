using System.Text.Json.Serialization;
using Wording.Core.Learning;

namespace Wording.Core;

public sealed class Word
{
    /// <summary>GUID, zeby dwa urzadzenia dodajace slowko offline nie wygenerowaly tego samego identyfikatora.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Original { get; set; } = string.Empty;

    public string Translation { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Stan powtorek. Wedruje razem ze slowkiem, zeby nauka byla spojna miedzy urzadzeniami.</summary>
    public ReviewState Review { get; set; } = new();

    /// <summary>Slowko jeszcze nigdy nieocenione.</summary>
    [JsonIgnore]
    public bool IsNew => Review.LastReviewedUtc is null;

    public bool IsDue(DateTimeOffset now) => Review.DueUtc <= now;
}
