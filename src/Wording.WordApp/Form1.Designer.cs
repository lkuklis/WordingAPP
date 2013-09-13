namespace Wording.WordApp
{
    partial class Form1
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
            this.dataGridWords = new System.Windows.Forms.DataGridView();
            this.btnAddNewWord = new System.Windows.Forms.Button();
            this.btnSaveWords = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridWords)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridWords
            // 
            this.dataGridWords.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridWords.Location = new System.Drawing.Point(12, 12);
            this.dataGridWords.Name = "dataGridWords";
            this.dataGridWords.Size = new System.Drawing.Size(524, 413);
            this.dataGridWords.TabIndex = 0;
            this.dataGridWords.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dataGridWords_RowsRemoved);
            // 
            // btnAddNewWord
            // 
            this.btnAddNewWord.Location = new System.Drawing.Point(461, 431);
            this.btnAddNewWord.Name = "btnAddNewWord";
            this.btnAddNewWord.Size = new System.Drawing.Size(75, 23);
            this.btnAddNewWord.TabIndex = 1;
            this.btnAddNewWord.Text = "Add";
            this.btnAddNewWord.UseVisualStyleBackColor = true;
            this.btnAddNewWord.Click += new System.EventHandler(this.btnAddNewWord_Click);
            // 
            // btnSaveWords
            // 
            this.btnSaveWords.Location = new System.Drawing.Point(12, 431);
            this.btnSaveWords.Name = "btnSaveWords";
            this.btnSaveWords.Size = new System.Drawing.Size(75, 23);
            this.btnSaveWords.TabIndex = 2;
            this.btnSaveWords.Text = "Save";
            this.btnSaveWords.UseVisualStyleBackColor = true;
            this.btnSaveWords.Click += new System.EventHandler(this.btnSaveWords_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 460);
            this.Controls.Add(this.btnSaveWords);
            this.Controls.Add(this.btnAddNewWord);
            this.Controls.Add(this.dataGridWords);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridWords)).EndInit();
            this.ResumeLayout(false);

        }



        #endregion

        private System.Windows.Forms.DataGridView dataGridWords;
        private System.Windows.Forms.Button btnAddNewWord;
        private System.Windows.Forms.Button btnSaveWords;
    }
}

