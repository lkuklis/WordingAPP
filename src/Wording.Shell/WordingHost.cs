using Wording.Core;
using Wording.Core.Storage;

namespace Wording.Shell;

/// <summary>
/// Punkt kompozycji wspolny dla wszystkich powlok UI.
/// <para>
/// Kluczowe jest to, ze magazyn powstaje tu raz i jest wspoldzielony przez caly
/// proces - to wlasnie brak tego powodowal, ze okno glowne i okienko dodawania
/// pisaly przez osobne kopie danych w pamieci.
/// </para>
/// </summary>
public sealed class WordingHost
{
    WordingHost(WordingSettings settings, JsonWordStore store, WordManager manager)
    {
        Settings = settings;
        Store = store;
        Manager = manager;
    }

    public WordingSettings Settings { get; }

    public JsonWordStore Store { get; }

    public WordManager Manager { get; }

    /// <summary>Sciezka pliku, z ktorego faktycznie korzysta aplikacja - przydatna w UI i diagnostyce.</summary>
    public string DataFilePath => Store.Path;

    public static WordingHost Create(TimeProvider? clock = null)
    {
        var zegar = clock ?? TimeProvider.System;
        var ustawienia = WordingSettings.Load();

        var magazyn = JsonWordStore.OpenOrMigrate(
            ustawienia.ResolveDataFile(),
            WordingSettings.FindLegacyXml(),
            zegar);

        return new WordingHost(ustawienia, magazyn, new WordManager(magazyn, zegar));
    }
}
