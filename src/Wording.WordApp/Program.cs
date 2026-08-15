using System;
using System.Windows.Forms;

namespace Wording.WordApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Jeden magazyn i jeden manager na caly proces - patrz WordingHost.
        var host = WordingHost.Create();

        Application.Run(new WordingMain(host.Manager, host.Settings));
    }
}
