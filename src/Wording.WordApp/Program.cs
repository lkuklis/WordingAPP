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

        var settings = WordingSettings.Load();
        var state = new WordingState(WordingState.DirectoryFor(settings));

        // One store and one manager for the whole process - every screen gets the same
        // instance, otherwise they would write through separate in-memory copies.
        // Switching sets replaces both, and only WordingMain does it.
        var ownWords = settings.ResolveDataFile();
        var active = WordSetCatalog.ResolveActiveFile(state.ActiveSetId, ownWords);
        var store = new JsonWordStore(active);

        // Only the user's own file is a migration target; an imported set was never a
        // pre-2026 install.
        if (active == ownWords)
        {
            store.ImportLegacyIfEmpty(WordingSettings.FindLegacyXml());
        }

        Application.Run(new WordingMain(new WordManager(store), settings, state));
    }
}
