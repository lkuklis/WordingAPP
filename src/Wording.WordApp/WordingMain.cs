using System;
using System.Linq;
using System.Windows.Forms;
using Wording.Core;
using Wording.Core.Learning;
using Wording.WordApp.Properties;

namespace Wording.WordApp;

public partial class WordingMain : Form
{
    /// <summary>Etykiety ocen w jednym miejscu - menu buduje sie z tej tablicy.</summary>
    static readonly (string Label, ReviewGrade Grade)[] Grades =
    [
        ("I know it", ReviewGrade.Good),
        ("Hard", ReviewGrade.Hard),
        ("Don't know", ReviewGrade.Again),
    ];

    readonly WordManager _manager;
    readonly NotifyIcon _notifyIcon;
    readonly System.Windows.Forms.Timer _timer;
    readonly int _showTimeMs;
    readonly ToolStripMenuItem[] _gradeItems;

    /// <summary>Ostatnio pokazane slowko - to jego dotycza oceny z menu w zasobniku.</summary>
    Word? _lastShown;

    public WordingMain(WordManager manager, WordingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(settings);

        InitializeComponent();

        _manager = manager;
        _showTimeMs = settings.ShowTimeSeconds * 1000;

        _gradeItems = [.. Grades.Select(grade =>
            new ToolStripMenuItem(grade.Label, null, (_, _) => Grade(grade.Grade)))];

        _notifyIcon = new NotifyIcon
        {
            Icon = Resources.Icon1,
            Text = "Wording",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu(),
        };
        _notifyIcon.MouseClick += NotifyIconMouseClick;
        _notifyIcon.BalloonTipClicked += (_, _) => ShowWindow();

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

        RefreshGrid();
    }

    ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.AddRange(_gradeItems);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Show window", null, (_, _) => ShowWindow()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Application.Exit()));

        menu.Opening += (_, _) => UpdateGradeItems();

        return menu;
    }

    void UpdateGradeItems()
    {
        var suffix = _lastShown is null ? string.Empty : $" — {_lastShown.Original}";

        for (var i = 0; i < _gradeItems.Length; i++)
        {
            _gradeItems[i].Enabled = _lastShown is not null;
            _gradeItems[i].Text = Grades[i].Label + suffix;
        }
    }

    void Grade(ReviewGrade grade)
    {
        if (_lastShown is null)
        {
            return;
        }

        _manager.Grade(_lastShown.Id, grade);
        _lastShown = null;

        RefreshGrid();
    }

    void RefreshGrid()
    {
        var now = DateTimeOffset.UtcNow;

        dataGridWords.AutoGenerateColumns = true;
        dataGridWords.DataSource = _manager.GetWords()
            .OrderBy(word => word.Review.DueUtc)
            .Select(word => new WordRow(word, now))
            .ToList();

        // Edycja komorek nigdy nie byla zapisywana, wiec siatka jest do odczytu.
        // Usuwanie wierszy dziala.
        dataGridWords.ReadOnly = true;

        if (dataGridWords.Columns[nameof(WordRow.Id)] is { } idColumn)
        {
            idColumn.Visible = false;
        }

        if (dataGridWords.Columns[nameof(WordRow.NextReview)] is { } dueColumn)
        {
            dueColumn.HeaderText = "Next review";
        }

        dataGridWords.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
    }

    void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    void HideOnMinimize(object sender, EventArgs e)
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
            ShowWindow();
        }
    }

    void ShowWordTick(object? sender, EventArgs e)
    {
        var word = _manager.NextWordToShow();

        if (word is null)
        {
            return;
        }

        _lastShown = word;
        _notifyIcon.ShowBalloonTip(_showTimeMs, word.Original, word.Translation, ToolTipIcon.Info);
    }

    void btnAddNewWord_Click(object sender, EventArgs e)
    {
        // Dialog dostaje ten sam manager, wiec nowe slowko widac od razu.
        using var dialog = new NewWord(_manager);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RefreshGrid();
        }
    }

    void dataGridWords_RowsRemoved(object sender, DataGridViewRowCancelEventArgs e)
    {
        if (e.Row?.DataBoundItem is WordRow row)
        {
            _manager.RemoveWord(row.Id);
        }
    }
}
