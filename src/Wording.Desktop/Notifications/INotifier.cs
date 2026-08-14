namespace Wording.Desktop.Notifications;

/// <summary>
/// Wyswietla powiadomienie systemowe. Avalonia nie ma tego w standardzie,
/// wiec kazdy system dostaje wlasna implementacje.
/// </summary>
public interface INotifier
{
    /// <summary>Czy na tym systemie potrafimy w ogole cokolwiek pokazac.</summary>
    bool IsSupported { get; }

    void Show(string title, string body);
}
