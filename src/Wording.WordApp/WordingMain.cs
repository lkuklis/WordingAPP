using System;
using System.Linq;
using System.Windows.Forms;
using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Packs;
using Wording.Core.Storage;
using Wording.WordApp.Properties;

namespace Wording.WordApp;

public partial class WordingMain : Form
{
    /// <summary>Grade labels in one place - the tray menu is built from this table.</summary>
    static readonly (string Label, ReviewGrade Grade)[] Grades =
    [
        ("I know it", ReviewGrade.Good),
        ("Hard", ReviewGrade.Hard),
        ("Don't know", ReviewGrade.Again),
    ];

    readonly NotifyIcon _notifyIcon;
    readonly System.Windows.Forms.Timer _timer;
    readonly int _showTimeMs;
    readonly ToolStripMenuItem[] _gradeItems;
    readonly ToolStripMenuItem _setsMenu;
    readonly WordingSettings _settings;
    readonly WordingState _state;

    /// <summary>
    /// Replaced when the user switches sets. Only this form does that, so there is still
    /// one active store per process - a screen left holding the previous manager would
    /// write through a stale in-memory copy.
    /// </summary>
    WordManager _manager;

    /// <summary>The word shown last - the tray menu grades apply to it.</summary>
    Word? _lastShown;

    public WordingMain(WordManager manager, WordingSettings settings, WordingState state)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(state);

        InitializeComponent();

        _manager = manager;
        _settings = settings;
        _state = state;
        _setsMenu = new ToolStripMenuItem("Learning set");
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

        // With no words the timer has nothing to show, so this is the only notification
        // a new user would ever get - it is what tells them the app is alive.
        if (_manager.GetWords().Count == 0)
        {
            _notifyIcon.ShowBalloonTip(
                _showTimeMs,
                "Wording is ready",
                "Add your first word from the tray menu and it will start showing up here.",
                ToolTipIcon.Info);
        }
    }

    ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.AddRange(_gradeItems);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_setsMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Show window", null, (_, _) => ShowWindow()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Application.Exit()));

        menu.Opening += (_, _) =>
        {
            UpdateGradeItems();

            // Rebuilt on every open: sets appear and disappear on disk, and a menu built
            // once at startup would keep offering one that has been deleted.
            RebuildSetsMenu();
        };

        return menu;
    }

    void RebuildSetsMenu()
    {
        _setsMenu.DropDownItems.Clear();
        _setsMenu.Text = $"Learning set — {ActiveSetName()}";

        _setsMenu.DropDownItems.Add(new ToolStripMenuItem("My words", null, (_, _) => SwitchTo(null))
        {
            Checked = _state.ActiveSetId is null,
        });

        var sets = WordSetCatalog.List();

        if (sets.Count > 0)
        {
            _setsMenu.DropDownItems.Add(new ToolStripSeparator());

            foreach (var set in sets)
            {
                var id = set.Id;

                _setsMenu.DropDownItems.Add(
                    new ToolStripMenuItem($"{set.Name} ({set.WordCount})", null, (_, _) => SwitchTo(id))
                    {
                        Checked = _state.ActiveSetId == id,
                    });
            }
        }

        _setsMenu.DropDownItems.Add(new ToolStripSeparator());
        _setsMenu.DropDownItems.Add(new ToolStripMenuItem("Import from a URL…", null, (_, _) => ImportFromUrl()));
    }

    /// <summary>Labels for the two sides, following the kind of the active set.</summary>
    (string Front, string Back) SideLabels()
    {
        if (_state.ActiveSetId is not { } id)
        {
            return ("Word", "Translation");
        }

        var set = WordSetCatalog.List().FirstOrDefault(candidate => candidate.Id == id);

        return PackKind.IsConcepts(set?.Kind) ? ("Term", "Definition") : ("Word", "Translation");
    }

    string ActiveSetName()
    {
        if (_state.ActiveSetId is not { } id)
        {
            return "My words";
        }

        return WordSetCatalog.List().FirstOrDefault(set => set.Id == id)?.Name ?? id;
    }

    void SwitchTo(string? setId)
    {
        if (setId == _state.ActiveSetId)
        {
            return;
        }

        var path = WordSetCatalog.ResolveActiveFile(setId, _settings.ResolveDataFile());

        _manager = new WordManager(new JsonWordStore(path));
        _state.Remember(setId);

        // The pending grade belongs to a word from the set being closed. Applying it
        // after the switch would either miss or, worse, hit an unrelated word.
        _lastShown = null;

        RefreshGrid();
    }

    void ImportFromUrl()
    {
        using var dialog = new ImportPack();
        dialog.ShowDialog(this);

        if (dialog.ImportedAnything)
        {
            RebuildSetsMenu();
        }
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

        var rows = _manager.GetWords()
            .OrderBy(word => word.Review.DueUtc)
            .Select(word => new WordRow(word, now))
            .ToList();

        dataGridWords.AutoGenerateColumns = true;
        dataGridWords.DataSource = rows;

        lblEmpty.Visible = rows.Count == 0;

        // Cell edits were never persisted, so the grid is read-only. Deleting rows works.
        dataGridWords.ReadOnly = true;

        if (dataGridWords.Columns[nameof(WordRow.Id)] is { } idColumn)
        {
            idColumn.Visible = false;
        }

        if (dataGridWords.Columns[nameof(WordRow.NextReview)] is { } dueColumn)
        {
            dueColumn.HeaderText = "Next review";
        }

        // A set of concepts is not a dictionary, and calling its definitions
        // "translations" makes the grid read as though something is missing.
        var (front, back) = SideLabels();

        if (dataGridWords.Columns[nameof(WordRow.Word)] is { } frontColumn)
        {
            frontColumn.HeaderText = front;
        }

        if (dataGridWords.Columns[nameof(WordRow.Translation)] is { } backColumn)
        {
            backColumn.HeaderText = back;
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
        // Right-click opens the context menu, so only react to the left button.
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
        // The dialog gets the same manager, so a new word shows up immediately.
        using var dialog = new NewWord(_manager);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RefreshGrid();
        }
    }

    void dataGridWords_RowsRemoved(object sender, DataGridViewRowCancelEventArgs e)
    {
        if (e.Row?.DataBoundItem is not WordRow row)
        {
            return;
        }

        _manager.RemoveWord(row.Id);

        // The grid moves the current row on by itself, so deleting a run of words takes
        // one keypress each. Only the empty-state label needs catching up, and it has to
        // wait until the row has actually gone - this event fires before that.
        BeginInvoke(() => lblEmpty.Visible = dataGridWords.Rows.Count == 0);

        if (_lastShown?.Id == row.Id)
        {
            _lastShown = null;
        }
    }

    void btnDeleteAll_Click(object sender, EventArgs e)
    {
        var count = _manager.GetWords().Count;

        if (count == 0)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"Delete all {count} words?\n\nTheir review progress goes with them. A copy of the "
                + "file is saved to the backups folder first, so this can still be undone by hand.",
            "Wording",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        var backup = _manager.RemoveAllWords();
        _lastShown = null;

        RefreshGrid();

        if (backup is not null)
        {
            MessageBox.Show(this, $"Backed up to:\n{backup}", "Wording", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
