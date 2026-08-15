using System;
using System.Windows.Forms;
using Wording.Core;

namespace Wording.WordApp;

public partial class NewWord : Form
{
    readonly WordManager _manager;

    /// <summary>The manager is injected so the dialog writes to the same store as the main window.</summary>
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

        // Setting DialogResult closes the modal dialog on its own.
        DialogResult = DialogResult.OK;
    }

    void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }
}
