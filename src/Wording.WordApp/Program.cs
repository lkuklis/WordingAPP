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

        // Jeden magazyn i jeden manager na caly proces - kazdy ekran dostaje
        // te sama instancje, inaczej pisalyby przez osobne kopie w pamieci.
        var store = new JsonWordStore(settings.ResolveDataFile());
        store.ImportLegacyIfEmpty(WordingSettings.FindLegacyXml());

        Application.Run(new WordingMain(new WordManager(store), settings));
    }
}
