namespace Wording.Core.Learning;

/// <summary>
/// Ocena, jaka uzytkownik wystawia sobie po zobaczeniu slowka.
/// Wartosci odpowiadaja skali jakosci z algorytmu SM-2 (0-5) i sa
/// przekazywane wprost do <see cref="SpacedRepetitionScheduler"/>.
/// Trzy stopnie, bo tyle przycisków da sie sensownie zmiescic w powiadomieniu.
/// </summary>
public enum ReviewGrade
{
    /// <summary>Nie pamietam - powtorki startuja od nowa.</summary>
    Again = 0,

    /// <summary>Z trudem, ale trafione - interwal rosnie, latwosc spada.</summary>
    Hard = 3,

    /// <summary>Pamietam bez wahania.</summary>
    Good = 5,
}
