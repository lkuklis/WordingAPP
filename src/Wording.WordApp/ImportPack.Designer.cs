namespace Wording.WordApp
{
    partial class ImportPack
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblCatalogue = new System.Windows.Forms.Label();
            this.listCatalogue = new System.Windows.Forms.ListView();
            this.btnReload = new System.Windows.Forms.Button();
            this.lblAddress = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.btnFetch = new System.Windows.Forms.Button();
            this.grpPreview = new System.Windows.Forms.GroupBox();
            this.lblName = new System.Windows.Forms.Label();
            this.lblDetail = new System.Windows.Forms.Label();
            this.btnImport = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.grpPreview.SuspendLayout();
            this.SuspendLayout();
            //
            // lblCatalogue
            //
            this.lblCatalogue.AutoSize = true;
            this.lblCatalogue.Location = new System.Drawing.Point(12, 12);
            this.lblCatalogue.Name = "lblCatalogue";
            this.lblCatalogue.Size = new System.Drawing.Size(300, 13);
            this.lblCatalogue.TabIndex = 10;
            this.lblCatalogue.Text = "Packs published with Wording — double-click one to download it";
            //
            // btnReload
            //
            this.btnReload.Location = new System.Drawing.Point(392, 7);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(75, 23);
            this.btnReload.TabIndex = 11;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            //
            // listCatalogue
            //
            this.listCatalogue.FullRowSelect = true;
            this.listCatalogue.HideSelection = false;
            this.listCatalogue.Location = new System.Drawing.Point(12, 34);
            this.listCatalogue.MultiSelect = false;
            this.listCatalogue.Name = "listCatalogue";
            this.listCatalogue.Size = new System.Drawing.Size(455, 150);
            this.listCatalogue.TabIndex = 12;
            this.listCatalogue.UseCompatibleStateImageBehavior = false;
            this.listCatalogue.View = System.Windows.Forms.View.Details;
            this.listCatalogue.DoubleClick += new System.EventHandler(this.listCatalogue_DoubleClick);
            //
            // lblAddress
            //
            this.lblAddress.AutoSize = true;
            this.lblAddress.Location = new System.Drawing.Point(12, 199);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(48, 13);
            this.lblAddress.TabIndex = 0;
            this.lblAddress.Text = "Address:";
            //
            // txtAddress
            //
            this.txtAddress.Location = new System.Drawing.Point(66, 196);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(320, 20);
            this.txtAddress.TabIndex = 1;
            this.txtAddress.PlaceholderText = "https://example.com/words.json";
            //
            // btnFetch
            //
            this.btnFetch.Location = new System.Drawing.Point(392, 194);
            this.btnFetch.Name = "btnFetch";
            this.btnFetch.Size = new System.Drawing.Size(75, 23);
            this.btnFetch.TabIndex = 2;
            this.btnFetch.Text = "Fetch";
            this.btnFetch.UseVisualStyleBackColor = true;
            this.btnFetch.Click += new System.EventHandler(this.btnFetch_Click);
            //
            // grpPreview
            //
            this.grpPreview.Controls.Add(this.lblName);
            this.grpPreview.Controls.Add(this.lblDetail);
            this.grpPreview.Controls.Add(this.btnImport);
            this.grpPreview.Location = new System.Drawing.Point(12, 229);
            this.grpPreview.Name = "grpPreview";
            this.grpPreview.Size = new System.Drawing.Size(455, 130);
            this.grpPreview.TabIndex = 3;
            this.grpPreview.TabStop = false;
            this.grpPreview.Text = "Pack";
            this.grpPreview.Visible = false;
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblName.Location = new System.Drawing.Point(12, 25);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(0, 20);
            this.lblName.TabIndex = 0;
            //
            // lblDetail
            //
            this.lblDetail.Location = new System.Drawing.Point(12, 50);
            this.lblDetail.Name = "lblDetail";
            this.lblDetail.Size = new System.Drawing.Size(431, 45);
            this.lblDetail.TabIndex = 1;
            //
            // btnImport
            //
            this.btnImport.Location = new System.Drawing.Point(318, 98);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(125, 23);
            this.btnImport.TabIndex = 2;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            //
            // lblStatus
            //
            this.lblStatus.Location = new System.Drawing.Point(12, 369);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(455, 45);
            this.lblStatus.TabIndex = 4;
            //
            // btnClose
            //
            this.btnClose.Location = new System.Drawing.Point(392, 422);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // ImportPack
            //
            this.AcceptButton = this.btnFetch;
            this.CancelButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(479, 455);
            this.Controls.Add(this.lblCatalogue);
            this.Controls.Add(this.btnReload);
            this.Controls.Add(this.listCatalogue);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.txtAddress);
            this.Controls.Add(this.btnFetch);
            this.Controls.Add(this.grpPreview);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ImportPack";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import a word pack";
            this.grpPreview.ResumeLayout(false);
            this.grpPreview.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblCatalogue;
        private System.Windows.Forms.ListView listCatalogue;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Button btnFetch;
        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblDetail;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnClose;
    }
}
