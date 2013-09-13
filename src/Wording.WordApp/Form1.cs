using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Wording.Core;
using Wording.WordApp.Properties;
using System.Configuration;

namespace Wording.WordApp
{
    public partial class Form1 : Form
    {
        private readonly NotifyIcon _notifyIcon1;
        private IEnumerable<Word> _words;
        private readonly WordManager _wp;
        readonly int _showTime = 1000 * Convert.ToInt32(ConfigurationManager.AppSettings["changeTime"]);
        readonly int _changeTime = Convert.ToInt32(ConfigurationManager.AppSettings["changeTime"]);

        public Form1()
        {
            InitializeComponent();

            _wp = new WordManager();

            RefreshAndBindDataSource();

            _notifyIcon1 = new NotifyIcon();
            _notifyIcon1.Click += notifyIcon1_Click;
            var timer1 = new Timer();
            timer1.Tick += timer1_Tick;


            _words = _wp.GetWords();

            _notifyIcon1.Icon = Resources.Icon1;
            _notifyIcon1.Visible = true;

            timer1.Interval = 1000 * _changeTime;
            timer1.Start();
        }

        private void RefreshAndBindDataSource()
        {

            var bindingSource = new BindingSource { DataSource = _wp.GetWordsData() };
            dataGridWords.AutoGenerateColumns = true;
            dataGridWords.DataSource = bindingSource;
            dataGridWords.Columns[0].ReadOnly = true;

            dataGridWords.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;

            _words = _wp.GetWords();
        }

        private void btnSaveWords_Click(object sender, EventArgs e)
        {
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int randomId = _words.GetRandomElement().Id;
            Word randomWord = _words.Single(w => w.Id == randomId);
            _notifyIcon1.ShowBalloonTip(_showTime, randomWord.OriginalValue,
                                        string.Format("{0}", randomWord.TranslationValue),
                                        ToolTipIcon.Info);
        }

        private void btnAddNewWord_Click(object sender, EventArgs e)
        {
            var newWordForm = new NewWord();
            var status = newWordForm.ShowDialog(this);
            if (status == DialogResult.OK)
            {
                RefreshAndBindDataSource();

            }
        }

        private void dataGridWords_RowsRemoved(object sender, DataGridViewRowCancelEventArgs e)
        {
            int id = (int)e.Row.Cells[0].Value;
            _wp.RemoveWord(id);
            RefreshAndBindDataSource();
        }
    }
}