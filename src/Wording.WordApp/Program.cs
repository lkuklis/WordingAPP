using System;
using System.Windows.Forms;
using Wording.Core;
using Wording.Core.Storage;

namespace Wording.WordApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var ustawienia = WordingSettings.Load();

        // Jeden magazyn na caly proces. Wczesniej kazdy ekran tworzyl wlasny,
        // wiec okna pisaly przez osobne kopie danych w pamieci.
        var magazyn = JsonWordStore.OpenOrMigrate(
            ustawienia.ResolveDataFile(),
            WordingSettings.FindLegacyXml(),
            TimeProvider.System);

        var manager = new WordManager(magazyn);

        Application.Run(new WordingMain(manager, ustawienia));
    }
}
