namespace Wording.Desktop.Notifications;

/// <summary>
/// Zaslepka dla systemow, dla ktorych ta powloka nie ma jeszcze powiadomien.
/// Na Windows produkcyjna sciezka to Wording.WordApp, a docelowo toasty
/// z Windows App SDK.
/// </summary>
public sealed class NullNotifier : INotifier
{
    public bool IsSupported => false;

    public void Show(string title, string body)
    {
    }
}
