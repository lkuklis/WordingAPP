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

            var bindingSource = new BindingSource { DataSource = _wp.GetWordsData() };
            dataGridWords.AutoGenerateColumns = true;
            dataGridWords.DataSource = bindingSource;

            dataGridWords.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;

            _notifyIcon1 = new NotifyIcon();
            _notifyIcon1.Click += notifyIcon1_Click;
            var timer1 = new Timer();
            timer1.Tick += timer1_Tick;


            _words = _wp.GetWords();

            _notifyIcon1.Icon = Resources.Icon1;
            _notifyIcon1.Visible = true;
            int changeTime = Convert.ToInt32(System.Configuration.ConfigurationSettings.AppSettings["changeTime"]);
            timer1.Interval = 1000*changeTime;
            timer1.Start();
        }

        private void btnSaveWords_Click(object sender, EventArgs e)
        {
            _wp.Save();
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

        private List<int> lastNumbers2 = new List<int>();

        private int GetWordNumber(int randomId = 0)
        {
            if (randomId == 0)
            {
                randomId = new Random().Next(1, _words.Count());
            }
            lastNumbers2.Capacity = 10;
            int lastindex = lastNumbers2.Count > 0 ? lastNumbers2.IndexOf(lastNumbers2.Last()):0;
            if (lastindex == lastNumbers2.Capacity - 1)
            {
                lastNumbers2.RemoveAt(0);
            }

            if (lastNumbers2.Contains(randomId))
            {
                if (lastNumbers2.Count == lastNumbers2.Capacity && lastNumbers2.FindIndex(a => a == randomId) < 3)
                {
                    lastNumbers2.RemoveAt(lastNumbers2.FindIndex(a => a == randomId));
                }

                return GetWordNumber(randomId+1);
            }

            lastNumbers2.Add(randomId);
            return randomId;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int randomId = GetWordNumber();
            if (randomId > _words.Count())
            {
                randomId = 1;
            }
            Word randomWord = _words.SingleOrDefault(w => w.Id * (-1) == randomId);
            if (randomWord != null)
            {
                _notifyIcon1.ShowBalloonTip(6000, randomWord.OrginalValue,
                                            string.Format("{0}", randomWord.TranslationValue),
                                            ToolTipIcon.Info);
            }
            else
            {
                timer1_Tick(this, null);
            }
        }
    }
}