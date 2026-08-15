using System.Diagnostics;

namespace Wording.Desktop.Notifications;

/// <summary>
/// Powiadomienia na macOS.
/// <para>
/// UWAGA na osascript: "display notification" konczy sie kodem 0 nawet wtedy,
/// gdy powiadomienie nie zostalo pokazane. Skrypt dziala z tozsamoscia Script
/// Editora i jesli ta aplikacja nie ma zgody na powiadomienia, system po cichu
/// je odrzuca. Zerowy kod wyjscia NIE jest dowodem dostarczenia.
/// </para>
/// <para>
/// Dlatego pierwszym wyborem jest terminal-notifier, ktory jest normalnym
/// zapakowanym .app z wlasnym identyfikatorem, wiec dostaje wlasny wpis w
/// ustawieniach powiadomien i faktycznie je pokazuje. osascript zostaje jako
/// awaryjny fallback.
/// </para>
/// <para>
/// Docelowo obie sciezki maja zniknac na rzecz UNUserNotificationCenter
/// wolanego z zapakowanego Wording.app - tylko to daje przyciski akcji,
/// czyli ocene powtorki wprost z powiadomienia.
/// </para>
/// </summary>
public sealed class MacNotifier : INotifier
{
    /// <summary>Miejsca, w ktorych Homebrew instaluje terminal-notifier.</summary>
    static readonly string[] ZnaneSciezki =
    [
        "/opt/homebrew/bin/terminal-notifier",
        "/usr/local/bin/terminal-notifier",
    ];

    readonly string? _terminalNotifier;

    public MacNotifier()
    {
        _terminalNotifier = OperatingSystem.IsMacOS() ? ZnajdzTerminalNotifier() : null;
    }

    public bool IsSupported => OperatingSystem.IsMacOS();

    /// <summary>Opis uzywanego mechanizmu - pokazywany w oknie, zeby nie diagnozowac po omacku.</summary>
    public string Strategy => _terminalNotifier is not null
        ? "terminal-notifier"
        : "osascript (moze byc po cichu blokowany)";

    public void Show(string title, string body)
    {
        if (!IsSupported)
        {
            return;
        }

        if (_terminalNotifier is not null)
        {
            Uruchom(_terminalNotifier, ["-title", title, "-message", body, "-group", "wording"]);
            return;
        }

        Uruchom("osascript", ["-e", $"display notification {Cytuj(body)} with title {Cytuj(title)}"]);
    }

    static string? ZnajdzTerminalNotifier()
    {
        var znane = Array.Find(ZnaneSciezki, File.Exists);

        if (znane is not null)
        {
            return znane;
        }

        // Poza znanymi lokalizacjami przeszukujemy PATH.
        var sciezki = Environment.GetEnvironmentVariable("PATH")?.Split(':') ?? [];

        foreach (var katalog in sciezki)
        {
            if (string.IsNullOrWhiteSpace(katalog))
            {
                continue;
            }

            var kandydat = Path.Combine(katalog, "terminal-notifier");

            if (File.Exists(kandydat))
            {
                return kandydat;
            }
        }

        return null;
    }

    static void Uruchom(string plik, string[] argumenty)
    {
        var uruchomienie = new ProcessStartInfo(plik)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // ArgumentList zamiast sklejania linii polecen - unika ucieczek na poziomie powloki.
        foreach (var argument in argumenty)
        {
            uruchomienie.ArgumentList.Add(argument);
        }

        try
        {
            using var proces = Process.Start(uruchomienie);
        }
        catch (Exception)
        {
            // Brak powiadomienia nie moze przewrocic aplikacji dzialajacej w tle.
        }
    }

    /// <summary>Zamienia tekst na literal lancuchowy AppleScript.</summary>
    static string Cytuj(string wartosc) =>
        "\"" + wartosc.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
