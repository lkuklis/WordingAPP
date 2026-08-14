using System.Text.Json.Serialization;

namespace Wording.Core.Storage;

/// <summary>
/// Kontekst serializacji generowany w czasie kompilacji.
/// <para>
/// Celowo zamiast serializacji przez refleksje: ta ostatnia przewraca sie w hostach,
/// ktore ja wylaczaja (System.Text.Json.JsonSerializerIsReflectionEnabledByDefault=false),
/// a takze pod trimmingiem i NativeAOT. Generator daje ten sam efekt bez refleksji.
/// </para>
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WordFile))]
internal sealed partial class WordJsonContext : JsonSerializerContext;
