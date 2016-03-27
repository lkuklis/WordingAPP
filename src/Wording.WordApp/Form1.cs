﻿using System;
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
        private readonly NotifyIcon _notifyIcon;
        private IEnumerable<Word> _words;
        private readonly WordManager _wordManager;
        readonly int _showTime = 1000 * Convert.ToInt32(ConfigurationManager.AppSettings["changeTime"]);
        readonly int _changeTime = Convert.ToInt32(ConfigurationManager.AppSettings["changeTime"]);

        public Form1()
        {
            InitializeComponent();

            _wordManager = new WordManager();

            RefreshAndBindDataSource();

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Click += NotifyIconClick;
            var timer = new Timer();
            timer.Tick += WordTick;


            _words = _wordManager.GetWords();

            _notifyIcon.Icon = Resources.Icon1;
            _notifyIcon.Visible = true;

            timer.Interval = 1000 * _changeTime;
            timer.Start();
        }

        private void RefreshAndBindDataSource()
        {
            var bindingSource = new BindingSource { DataSource = _wordManager.GetWordsData() };
            dataGridWords.AutoGenerateColumns = true;
            dataGridWords.DataSource = bindingSource;
            dataGridWords.Columns[0].ReadOnly = true;

            dataGridWords.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;

            _words = _wordManager.GetWords();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void NotifyIconClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void WordTick(object sender, EventArgs e)
        {
            var randomWord = _words.GetRandomElement();
            _notifyIcon.ShowBalloonTip(_showTime, randomWord.OriginalValue,
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
            _wordManager.RemoveWord(id);
            RefreshAndBindDataSource();
        }
    }
}