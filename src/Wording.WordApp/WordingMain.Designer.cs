namespace Wording.WordApp
{
    partial class WordingMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listSets = new System.Windows.Forms.ListView();
            this.btnImportSet = new System.Windows.Forms.Button();
            this.btnRemoveSet = new System.Windows.Forms.Button();
            this.dataGridWords = new System.Windows.Forms.DataGridView();
            this.btnAddNewWord = new System.Windows.Forms.Button();
            this.btnDeleteAll = new System.Windows.Forms.Button();
            this.lblEmpty = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridWords)).BeginInit();
            this.SuspendLayout();
            //
            // listSets
            //
            // The sets used to be reachable only from the tray menu, where they were easy
            // to miss and impossible to compare once there were more than a couple.
            this.listSets.FullRowSelect = true;
            this.listSets.HideSelection = false;
            this.listSets.Location = new System.Drawing.Point(12, 12);
            this.listSets.MultiSelect = false;
            this.listSets.Name = "listSets";
            this.listSets.Size = new System.Drawing.Size(210, 420);
            this.listSets.TabIndex = 0;
            this.listSets.UseCompatibleStateImageBehavior = false;
            this.listSets.View = System.Windows.Forms.View.Details;
            this.listSets.SelectedIndexChanged += new System.EventHandler(this.listSets_SelectedIndexChanged);
            //
            // btnImportSet
            //
            this.btnImportSet.Location = new System.Drawing.Point(12, 438);
            this.btnImportSet.Name = "btnImportSet";
            this.btnImportSet.Size = new System.Drawing.Size(100, 23);
            this.btnImportSet.TabIndex = 1;
            this.btnImportSet.Text = "Add set…";
            this.btnImportSet.UseVisualStyleBackColor = true;
            this.btnImportSet.Click += new System.EventHandler(this.btnImportSet_Click);
            //
            // btnRemoveSet
            //
            this.btnRemoveSet.Location = new System.Drawing.Point(118, 438);
            this.btnRemoveSet.Name = "btnRemoveSet";
            this.btnRemoveSet.Size = new System.Drawing.Size(104, 23);
            this.btnRemoveSet.TabIndex = 2;
            this.btnRemoveSet.Text = "Remove set";
            this.btnRemoveSet.UseVisualStyleBackColor = true;
            this.btnRemoveSet.Click += new System.EventHandler(this.btnRemoveSet_Click);
            //
            // dataGridWords
            //
            this.dataGridWords.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridWords.Location = new System.Drawing.Point(232, 12);
            this.dataGridWords.Name = "dataGridWords";
            this.dataGridWords.Size = new System.Drawing.Size(576, 420);
            this.dataGridWords.TabIndex = 3;
            this.dataGridWords.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dataGridWords_RowsRemoved);
            //
            // btnDeleteAll
            //
            this.btnDeleteAll.Location = new System.Drawing.Point(232, 438);
            this.btnDeleteAll.Name = "btnDeleteAll";
            this.btnDeleteAll.Size = new System.Drawing.Size(90, 23);
            this.btnDeleteAll.TabIndex = 4;
            this.btnDeleteAll.Text = "Delete all…";
            this.btnDeleteAll.UseVisualStyleBackColor = true;
            this.btnDeleteAll.Click += new System.EventHandler(this.btnDeleteAll_Click);
            //
            // btnAddNewWord
            //
            this.btnAddNewWord.Location = new System.Drawing.Point(733, 438);
            this.btnAddNewWord.Name = "btnAddNewWord";
            this.btnAddNewWord.Size = new System.Drawing.Size(75, 23);
            this.btnAddNewWord.TabIndex = 5;
            this.btnAddNewWord.Text = "Add";
            this.btnAddNewWord.UseVisualStyleBackColor = true;
            this.btnAddNewWord.Click += new System.EventHandler(this.btnAddNewWord_Click);
            //
            // lblEmpty
            //
            // Shown over the empty grid on a first run - nothing is seeded any more, so
            // without it the window is a blank table with no hint of what to do.
            this.lblEmpty.BackColor = System.Drawing.SystemColors.Window;
            this.lblEmpty.Location = new System.Drawing.Point(232, 12);
            this.lblEmpty.Name = "lblEmpty";
            this.lblEmpty.Size = new System.Drawing.Size(576, 420);
            this.lblEmpty.TabIndex = 6;
            this.lblEmpty.Text = "Nothing here yet.\r\n\r\nUse Add below to enter your first entry, or Add set… to downl" +
                "oad one, and Wording will start showing it in notifications.";
            this.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmpty.Visible = false;
            //
            // WordingMain
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(820, 473);
            this.Controls.Add(this.listSets);
            this.Controls.Add(this.btnImportSet);
            this.Controls.Add(this.btnRemoveSet);
            this.Controls.Add(this.btnDeleteAll);
            this.Controls.Add(this.btnAddNewWord);
            // lblEmpty is added before the grid so it sits in front of it.
            this.Controls.Add(this.lblEmpty);
            this.Controls.Add(this.dataGridWords);
            this.Name = "WordingMain";
            this.Text = "Wording";
            this.Resize += new System.EventHandler(this.HideOnMinimize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridWords)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ListView listSets;
        private System.Windows.Forms.Button btnImportSet;
        private System.Windows.Forms.Button btnRemoveSet;
        private System.Windows.Forms.DataGridView dataGridWords;
        private System.Windows.Forms.Button btnAddNewWord;
        private System.Windows.Forms.Button btnDeleteAll;
        private System.Windows.Forms.Label lblEmpty;
    }
}
