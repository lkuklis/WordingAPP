using System;
using System.Linq;
using System.Windows.Forms;
using Wording.Core;
using Wording.Core.Learning;
using Wording.WordApp.Properties;

namespace Wording.WordApp;

public partial class WordingMain : Form
{
    readonly WordManager _manager;
    readonly NotifyIcon _notifyIcon;
    readonly System.Windows.Forms.Timer _timer;
    readonly int _showTimeMs;

    readonly ToolStripMenuItem _oceanZnam;
    readonly ToolStripMenuItem _ocenTrudne;
    readonly ToolStripMenuItem _ocenNieZnam;

    /// <summary>Ostatnio pokazane slowko - to jego dotycza oceny z menu w zasobniku.</summary>
    Word? _ostatnioPokazane;

    public WordingMain(WordManager manager, WordingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(settings);

        InitializeComponent();

        _manager = manager;

        // Poprzednia wersja liczyla czas wyswietlania powiadomienia z klucza
        // changeTime, przez co showTime z konfiguracji nie robil zupelnie nic.
        _showTimeMs = settings.ShowTimeSeconds * 1000;

        _oceanZnam = new ToolStripMenuItem("I know it", null, (_, _) => Ocen(ReviewGrade.Good));
        _ocenTrudne = new ToolStripMenuItem("Hard", null, (_, _) => Ocen(ReviewGrade.Hard));
        _ocenNieZnam = new ToolStripMenuItem("Don't know", null, (_, _) => Ocen(ReviewGrade.Again));

        _notifyIcon = new NotifyIcon
        {
            Icon = Resources.Icon1,
            Text = "Wording",
            Visible = true,
            ContextMenuStrip = ZbudujMenuZasobnika(),
        };
        _notifyIcon.MouseClick += NotifyIconMouseClick;
        _notifyIcon.BalloonTipClicked += (_, _) => PokazOkno();

        _timer = new System.Windows.Forms.Timer { Interval = settings.ChangeTimeSeconds * 1000 };
        _timer.Tick += ShowWordTick;
        _timer.Start();

        FormClosed += (_, _) =>
        {
            _timer.Stop();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _timer.Dispose();
        };

        OdswiezSiatke();
    }

    ContextMenuStrip ZbudujMenuZasobnika()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(_oceanZnam);
        menu.Items.Add(_ocenTrudne);
        menu.Items.Add(_ocenNieZnam);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Show window", null, (_, _) => PokazOkno()));
        // Poprzednia wersja nie miala jak sie zamknac - okno tylko chowalo sie do
        // zasobnika, a proces trzeba bylo ubijac recznie.
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Application.Exit()));

        menu.Opening += (_, _) => UstawDostepnoscOcen();

        return menu;
    }

    void UstawDostepnoscOcen()
    {
        var mozna = _ostatnioPokazane is not null;

        _oceanZnam.Enabled = mozna;
        _ocenTrudne.Enabled = mozna;
        _ocenNieZnam.Enabled = mozna;

        var opis = _ostatnioPokazane is null ? string.Empty : $" — {_ostatnioPokazane.Original}";
        _oceanZnam.Text = "I know it" + opis;
        _ocenTrudne.Text = "Hard" + opis;
        _ocenNieZnam.Text = "Don't know" + opis;
    }

    void Ocen(ReviewGrade ocena)
    {
        if (_ostatnioPokazane is null)
        {
            return;
        }

        _manager.Grade(_ostatnioPokazane.Id, ocena);
        _ostatnioPokazane = null;

        OdswiezSiatke();
    }

    void OdswiezSiatke()
    {
        var teraz = DateTimeOffset.UtcNow;
        var wiersze = _manager.GetWords()
            .OrderBy(word => word.Review.DueUtc)
            .Select(word => new WordRow(word, teraz))
            .ToList();

        dataGridWords.AutoGenerateColumns = true;
        dataGridWords.DataSource = new BindingSource { DataSource = wiersze };

        // Edycja w siatce nigdy nie byla zapisywana na dysk, wiec zamiast
        // udawac, ze dziala, siatka jest do odczytu. Usuwanie wierszy dziala.
        dataGridWords.ReadOnly = true;

        if (dataGridWords.Columns[nameof(WordRow.Id)] is { } kolumnaId)
        {
            kolumnaId.Visible = false;
        }

        if (dataGridWords.Columns[nameof(WordRow.NextReview)] is { } kolumnaTerminu)
        {
            kolumnaTerminu.HeaderText = "Next review";
        }

        dataGridWords.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
    }

    void PokazOkno()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    void Form1_Resize(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    void NotifyIconMouseClick(object? sender, MouseEventArgs e)
    {
        // Prawy przycisk obsluguje menu kontekstowe, wiec reagujemy tylko na lewy.
        if (e.Button == MouseButtons.Left)
        {
            PokazOkno();
        }
    }

    void ShowWordTick(object? sender, EventArgs e)
    {
        var slowo = _manager.NextWordToShow();

        // Pusta lista konczyla sie wczesniej wyjatkiem przy kazdym tyknieciu zegara.
        if (slowo is null)
        {
            return;
        }

        _ostatnioPokazane = slowo;
        _notifyIcon.ShowBalloonTip(_showTimeMs, slowo.Original, slowo.Translation, ToolTipIcon.Info);
    }

    void btnAddNewWord_Click(object sender, EventArgs e)
    {
        // Dialog dostaje ten sam manager, wiec nowe slowko widac od razu,
        // bez ponownego czytania pliku z dysku.
        using var okienko = new NewWord(_manager);

        if (okienko.ShowDialog(this) == DialogResult.OK)
        {
            OdswiezSiatke();
        }
    }

    void dataGridWords_RowsRemoved(object sender, DataGridViewRowCancelEventArgs e)
    {
        if (e.Row?.DataBoundItem is WordRow wiersz)
        {
            _manager.RemoveWord(wiersz.Id);
        }
    }
}
