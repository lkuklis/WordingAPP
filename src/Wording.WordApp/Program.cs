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

        // One store and one manager for the whole process - every screen gets the
        // same instance, otherwise they would write through separate in-memory copies.
        var store = new JsonWordStore(settings.ResolveDataFile());
        store.ImportLegacyIfEmpty(WordingSettings.FindLegacyXml());

        Application.Run(new WordingMain(new WordManager(store), settings));
    }
}
