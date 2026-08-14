using System.Collections.ObjectModel;
using Avalonia.Controls;
using Wording.Core;
using Wording.Core.Learning;
using Wording.Shell;

namespace Wording.Desktop;

public partial class MainWindow : Window
{
    readonly WordManager _manager;
    readonly ObservableCollection<WordRow> _wiersze = [];

    public MainWindow(WordManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        _manager = manager;

        InitializeComponent();

        WordsList.ItemsSource = _wiersze;

        AddButton.Click += (_, _) => Dodaj();
        TranslationBox.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                Dodaj();
            }
        };

        GoodButton.Click += (_, _) => OcenZaznaczone(ReviewGrade.Good);
        HardButton.Click += (_, _) => OcenZaznaczone(ReviewGrade.Hard);
        AgainButton.Click += (_, _) => OcenZaznaczone(ReviewGrade.Again);
        DeleteButton.Click += (_, _) => UsunZaznaczone();

        // Aplikacja zyje w pasku menu, wiec zamkniecie okna ma je tylko schowac.
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };

        Odswiez();
    }

    /// <summary>Przeladowuje liste - wolane takze po ocenie z paska menu.</summary>
    public void Odswiez()
    {
        var zaznaczoneId = (WordsList.SelectedItem as WordRow)?.Id;
        var teraz = DateTimeOffset.UtcNow;

        _wiersze.Clear();

        foreach (var slowo in _manager.GetWords().OrderBy(word => word.Review.DueUtc))
        {
            _wiersze.Add(new WordRow(slowo, teraz));
        }

        if (zaznaczoneId is { } id)
        {
            WordsList.SelectedItem = _wiersze.FirstOrDefault(wiersz => wiersz.Id == id);
        }

        StatusText.Text = $"{_wiersze.Count} words  ·  {_manager.GetWords().Count(w => w.Review.DueUtc <= teraz)} due now";
    }

    public void PokazIAktywuj()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    void Dodaj()
    {
        try
        {
            _manager.AddWord(OriginalBox.Text ?? string.Empty, TranslationBox.Text ?? string.Empty);
        }
        catch (ArgumentException)
        {
            // Puste pola - nie ma czego dodawac, zostawiamy tekst do poprawki.
            return;
        }

        OriginalBox.Text = string.Empty;
        TranslationBox.Text = string.Empty;
        OriginalBox.Focus();

        Odswiez();
    }

    void OcenZaznaczone(ReviewGrade ocena)
    {
        if (WordsList.SelectedItem is not WordRow wiersz)
        {
            return;
        }

        _manager.Grade(wiersz.Id, ocena);
        Odswiez();
    }

    void UsunZaznaczone()
    {
        if (WordsList.SelectedItem is not WordRow wiersz)
        {
            return;
        }

        _manager.RemoveWord(wiersz.Id);
        Odswiez();
    }
}
