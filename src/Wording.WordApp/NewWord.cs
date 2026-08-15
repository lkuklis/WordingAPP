using System;
using System.Windows.Forms;
using Wording.Core;

namespace Wording.WordApp;

public partial class NewWord : Form
{
    readonly WordManager _manager;

    /// <summary>Manager przychodzi z zewnatrz, zeby dialog pisal do tego samego magazynu co okno glowne.</summary>
    public NewWord(WordManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        InitializeComponent();

        _manager = manager;
    }

    void btnSave_Click(object sender, EventArgs e)
    {
        try
        {
            _manager.AddWord(txtOriginal.Text, txtTranslation.Text);
        }
        catch (ArgumentException)
        {
            MessageBox.Show(
                this,
                "Both the word and its translation are required.",
                "Wording",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Ustawienie DialogResult samo zamyka okno modalne.
        DialogResult = DialogResult.OK;
    }

    void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }
}
