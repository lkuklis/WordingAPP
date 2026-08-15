using System.Text.Json.Serialization;
using Wording.Core.Learning;

namespace Wording.Core;

public sealed class Word
{
    /// <summary>A GUID, so two devices adding a word offline cannot produce the same identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Original { get; set; } = string.Empty;

    public string Translation { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Review state. It travels with the word so progress stays consistent across devices.</summary>
    public ReviewState Review { get; set; } = new();

    /// <summary>A word that has never been graded.</summary>
    [JsonIgnore]
    public bool IsNew => Review.LastReviewedUtc is null;

    public bool IsDue(DateTimeOffset now) => Review.DueUtc <= now;
}
