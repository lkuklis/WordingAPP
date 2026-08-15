using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wording.Core.Packs;

namespace Wording.WordApp;

/// <summary>
/// Downloads a word pack and shows what it is before any of it is written.
/// <para>
/// The preview step is the point of the dialog. A pack comes from an address the user
/// pasted, so the first chance to see whose words these are and how many of them there
/// are should come before they land on disk, not after.
/// </para>
/// </summary>
public partial class ImportPack : Form
{
    readonly PackDownloader _downloader = new();
    readonly WordPackImporter _importer = new();

    WordPack? _pack;
    Uri? _address;

    /// <summary>True once something was imported, so the caller can refresh its menu.</summary>
    public bool ImportedAnything { get; private set; }

    public ImportPack()
    {
        InitializeComponent();
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

        try
        {
            var result = _importer.Import(_pack, _address, replaceExisting: _importer.Exists(_pack));

            ImportedAnything = true;
            grpPreview.Visible = false;
            _pack = null;

            Report(result.Added == 0
                ? $"Nothing new to add - you already have every word in {result.Set.Name}."
                : $"Added {result.Added} words to {result.Set.Name}. Pick it under Learning set to start.");
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
