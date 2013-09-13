using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Wording.Core;
using Wording.WordApp.Properties;

namespace Wording.WordApp
{
    public partial class Form1 : Form
    {
        private readonly NotifyIcon _notifyIcon1;
        private readonly IEnumerable<Word> _words;
        private readonly WordManager _wp;


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
            int changeTime = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["changeTime"]);
            timer1.Interval = 1000 * changeTime;
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

        private readonly List<int> _numberList = new List<int>();

        private int GetWordNumber(int randomId = 0)
        {
            if (randomId == 0)
            {
                randomId = new Random().Next(1, _words.Count());
            }
            _numberList.Capacity = 10;
            int lastindex = _numberList.Count > 0 ? _numberList.IndexOf(_numberList.Last()) : 0;
            if (lastindex == _numberList.Capacity - 1)
            {
                _numberList.RemoveAt(0);
            }

            if (_numberList.Contains(randomId))
            {
                if (_numberList.Count == _numberList.Capacity && _numberList.FindIndex(a => a == randomId) < 3)
                {
                    _numberList.RemoveAt(_numberList.FindIndex(a => a == randomId));
                }

                return GetWordNumber(randomId + 1);
            }

            _numberList.Add(randomId);
            return randomId;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int randomId = GetWordNumber();
            if (randomId > _words.Count())
            {
                randomId = 1;
            }
            Word randomWord = _words.SingleOrDefault(w => w.Id == randomId);
            if (randomWord != null)
            {
                _notifyIcon1.ShowBalloonTip(6000, randomWord.OriginalValue,
                                            string.Format("{0}", randomWord.TranslationValue),
                                            ToolTipIcon.Info);
            }
            else
            {
                timer1_Tick(this, null);
            }
        }

        private void dataGridWords_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

            
            

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