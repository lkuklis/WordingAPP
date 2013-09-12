using System;
using System.Windows.Forms;

namespace Wording.WordApp
{
    using Wording.Core;

    public partial class NewWord : Form
    {
        private WordManager _wp;

        public NewWord()
        {
            InitializeComponent();
            _wp = new WordManager();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var original = txtOriginal.Text;
            var translation = txtTranslation.Text;
            _wp.AddWord(original, translation);
            this.DialogResult = DialogResult.OK;
            this.Dispose();

            

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Dispose();

        }
    }
}
