using System;
using System.Windows.Forms;
using Wording.Core;

namespace Wording.WordApp;

public partial class NewWord : Form
{
    readonly WordManager _manager;

    /// <summary>
    /// Manager przychodzi z zewnatrz - dialog celowo nie tworzy wlasnego,
    /// bo wtedy pisalby przez osobna kopie danych w pamieci.
    /// </summary>
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
        catch (ArgumentException wyjatek)
        {
            MessageBox.Show(this, wyjatek.Message, "Wording", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Ustawienie DialogResult samo zamyka okno modalne; wolanie Dispose()
        // w tym miejscu, jak robila poprzednia wersja, niszczylo formularz
        // jeszcze w trakcie obslugi zdarzenia.
        DialogResult = DialogResult.OK;
    }

    void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }
}
