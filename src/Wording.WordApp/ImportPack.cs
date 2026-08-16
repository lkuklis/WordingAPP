using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wording.Core.Packs;

namespace Wording.WordApp;

/// <summary>
/// Two ways in: the published catalogue, and any other address.
/// <para>
/// The catalogue is fetched when the window opens, so the list is current; the app makes
/// no network request until then. Downloading a pack from an address the user pasted
/// still shows a preview before anything is written, because there is nothing else
/// vouching for it - the catalogue's packs are validated in CI before they are published,
/// which is why a double click there imports straight away.
/// </para>
/// </summary>
public partial class ImportPack : Form
{
    readonly PackDownloader _downloader = new();
    readonly WordPackImporter _importer = new();

    Uri? _indexUrl;
    IReadOnlyList<PackIndexEntry> _catalogue = [];

    WordPack? _pack;
    Uri? _address;

    /// <summary>True once something was imported, so the caller can refresh its menu.</summary>
    public bool ImportedAnything { get; private set; }

    /// <summary>
    /// The set imported last. Downloading a set is asking to learn from it, so the main
    /// window switches to it when this dialog closes rather than leaving it to be found
    /// in a menu.
    /// </summary>
    public string? ImportedSetId { get; private set; }

    public ImportPack()
    {
        InitializeComponent();

        listCatalogue.Columns.Add("Pack", 250);
        listCatalogue.Columns.Add("Entries", 70);
        listCatalogue.Columns.Add("", 110);

        // Fetched when the window opens, so the list is current. This is the app's only
        // unprompted network request, which is why the label says where it comes from.
        Shown += async (_, _) => await LoadCatalogueAsync();
    }

    async void btnReload_Click(object sender, EventArgs e) => await LoadCatalogueAsync();

    async Task LoadCatalogueAsync()
    {
        btnReload.Enabled = false;
        listCatalogue.Items.Clear();
        Report("Loading the catalogue…");

        try
        {
            _indexUrl = new Uri(PackSource.OfficialIndexUrl);
            _catalogue = await new PackDownloader().DownloadIndexAsync(_indexUrl).ConfigureAwait(true);

            ShowCatalogue();
            Report(string.Empty);
        }
        catch (WordPackException error)
        {
            Report("Could not load the catalogue. " + Describe(error));
        }
        finally
        {
            btnReload.Enabled = true;
        }
    }

    void ShowCatalogue()
    {
        listCatalogue.BeginUpdate();
        listCatalogue.Items.Clear();

        foreach (var entry in _catalogue)
        {
            var unit = PackKind.IsConcepts(entry.Kind) ? "terms" : "words";

            var row = new ListViewItem(entry.Name);
            row.SubItems.Add($"{entry.WordCount} {unit}");
            row.SubItems.Add(_importer.SetExists(entry.Id) ? "Installed" : string.Empty);
            row.ToolTipText = entry.Description;
            row.Tag = entry;

            listCatalogue.Items.Add(row);
        }

        listCatalogue.EndUpdate();
    }

    async void listCatalogue_DoubleClick(object sender, EventArgs e)
    {
        if (listCatalogue.SelectedItems.Count == 0
            || listCatalogue.SelectedItems[0].Tag is not PackIndexEntry entry
            || _indexUrl is null)
        {
            return;
        }

        var replaceExisting = _importer.SetExists(entry.Id);

        if (replaceExisting)
        {
            var answer = MessageBox.Show(
                this,
                $"You already have {entry.Name}.\n\nAdd the entries it does not have yet? "
                    + "Your review progress on the rest is kept.",
                "Wording",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }
        }

        listCatalogue.Enabled = false;
        Report($"Downloading {entry.Name}…");

        try
        {
            // Built from the entry's identifier and the catalogue's own address, so the
            // file being downloaded cannot choose where the app looks.
            var url = PackSource.PackUrl(_indexUrl, entry.Id);
            var pack = await new PackDownloader().DownloadAsync(url).ConfigureAwait(true);

            Install(pack, url, replaceExisting);
            ShowCatalogue();
        }
        catch (WordPackException error)
        {
            Report(Describe(error));
        }
        finally
        {
            listCatalogue.Enabled = true;
        }
    }

    async void btnFetch_Click(object sender, EventArgs e)
    {
        if (!Uri.TryCreate(txtAddress.Text.Trim(), UriKind.Absolute, out var address))
        {
            Report("That is not a web address.");
            return;
        }

        _pack = null;
        _address = null;
        grpPreview.Visible = false;
        Report("Downloading…");

        // The download must not block the message loop, or the window freezes mid-fetch.
        btnFetch.Enabled = false;

        try
        {
            var pack = await _downloader.DownloadAsync(address).ConfigureAwait(true);

            _pack = pack;
            _address = address;

            ShowPreview(pack);
            Report(string.Empty);
        }
        catch (WordPackException error)
        {
            Report(Describe(error));
        }
        finally
        {
            btnFetch.Enabled = true;
        }
    }

    void ShowPreview(WordPack pack)
    {
        var known = _importer.Exists(pack);

        lblName.Text = pack.Name;

        var detail = $"{pack.Words.Count} words.";

        if (!string.IsNullOrWhiteSpace(pack.Description))
        {
            detail = pack.Description + Environment.NewLine + detail;
        }

        if (known)
        {
            // Never a silent overwrite: an existing set holds review progress.
            detail += Environment.NewLine
                + "You already have this set. Importing adds only the words it does not have yet "
                + "and leaves your progress on the rest alone.";
        }

        lblDetail.Text = detail;
        btnImport.Text = known ? "Add new words" : "Import as a new set";
        grpPreview.Visible = true;
    }

    void btnImport_Click(object sender, EventArgs e)
    {
        if (_pack is null || _address is null)
        {
            return;
        }

        Install(_pack, _address, _importer.Exists(_pack));

        grpPreview.Visible = false;
        _pack = null;
    }

    /// <summary>Shared by the catalogue and the address box - the write is the same.</summary>
    void Install(WordPack pack, Uri source, bool replaceExisting)
    {
        try
        {
            var result = _importer.Import(pack, source, replaceExisting);

            ImportedAnything = true;
            ImportedSetId = result.Set.Id;

            Report(result.Added == 0
                ? $"Nothing new to add - you already have every entry in {result.Set.Name}."
                : $"Added {result.Added} entries to {result.Set.Name}. Pick it under Learning set to start.");
        }
        catch (WordPackException error)
        {
            Report(Describe(error));
        }
    }

    void btnClose_Click(object sender, EventArgs e) => Close();

    void Report(string message) => lblStatus.Text = message;

    /// <summary>
    /// The wording lives here rather than in Wording.Core: the core raises typed cases
    /// and the UI decides how to say them.
    /// </summary>
    static string Describe(WordPackException error) => error.Problem switch
    {
        PackProblem.NotHttps => "The address has to start with https://",
        PackProblem.Network => "Could not download the pack. Check the address and your connection.",
        PackProblem.TooLarge => "That file is too big to be a word pack.",
        PackProblem.Malformed => "That file is not a word pack.",
        PackProblem.Empty => "The pack has no words in it.",
        PackProblem.UnsafeId => "The pack has an identifier Wording cannot use as a file name.",
        PackProblem.AlreadyExists => "You already have this pack.",
        _ => "The pack could not be imported.",
    };
}
