using System.Text.Json.Serialization;

namespace Wording.Core.Storage;

/// <summary>
/// Serialization context generated at compile time.
/// <para>
/// Deliberately used instead of reflection-based serialization: reflection throws in
/// hosts that disable it (System.Text.Json.JsonSerializerIsReflectionEnabledByDefault=false),
/// and breaks the same way under trimming and NativeAOT. The generator gives the same
/// result with no reflection at all.
/// </para>
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WordFile))]
[JsonSerializable(typeof(Packs.WordPack))]
internal sealed partial class WordJsonContext : JsonSerializerContext;
