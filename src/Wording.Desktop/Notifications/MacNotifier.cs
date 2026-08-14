using System.Diagnostics;

namespace Wording.Desktop.Notifications;

/// <summary>
/// Powiadomienia macOS przez osascript.
/// <para>
/// Swiadomie bez zadnej biblioteki zewnetrznej: jedyna gotowa paczka dla Avalonii
/// (DesktopNotifications) ma kilkadziesiat tysiecy pobran, a powiadomienia sa tu
/// calym produktem - nie chcemy na tym miejscu zaleznosci tej wielkosci.
/// </para>
/// <para>
/// Ograniczenie: "display notification" nie obsluguje przyciskow akcji, wiec ocena
/// powtorki idzie przez menu w pasku. Docelowy natywny port na macOS uzyje
/// UNUserNotificationCenter, ktore przyciski ma.
/// </para>
/// </summary>
public sealed class MacNotifier : INotifier
{
    public bool IsSupported => OperatingSystem.IsMacOS();

    public void Show(string title, string body)
    {
        if (!IsSupported)
        {
            return;
        }

        var skrypt = $"display notification {Cytuj(body)} with title {Cytuj(title)}";

        var uruchomienie = new ProcessStartInfo("osascript")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // ArgumentList zamiast sklejania linii polecen - unika ucieczek na poziomie powloki.
        uruchomienie.ArgumentList.Add("-e");
        uruchomienie.ArgumentList.Add(skrypt);

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
