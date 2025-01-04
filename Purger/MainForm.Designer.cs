namespace Purger;

partial class MainForm
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
		txtGameDir = new TextBox();
		label1 = new Label();
		btnBrowse = new Button();
		progressBar = new ProgressBar();
		btnPurge = new Button();
		folderDialog = new FolderBrowserDialog();
		SuspendLayout();
		// 
		// txtGameDir
		// 
		txtGameDir.Location = new Point(12, 27);
		txtGameDir.Name = "txtGameDir";
		txtGameDir.ReadOnly = true;
		txtGameDir.Size = new Size(222, 23);
		txtGameDir.TabIndex = 0;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(12, 9);
		label1.Name = "label1";
		label1.Size = new Size(89, 15);
		label1.TabIndex = 1;
		label1.Text = "Game Directory";
		// 
		// btnBrowse
		// 
		btnBrowse.Location = new Point(240, 27);
		btnBrowse.Name = "btnBrowse";
		btnBrowse.Size = new Size(32, 23);
		btnBrowse.TabIndex = 2;
		btnBrowse.Text = "...";
		btnBrowse.UseVisualStyleBackColor = true;
		btnBrowse.Click += btnBrowse_Click;
		// 
		// progressBar
		// 
		progressBar.Location = new Point(12, 56);
		progressBar.Name = "progressBar";
		progressBar.Size = new Size(260, 23);
		progressBar.Step = 5;
		progressBar.Style = ProgressBarStyle.Continuous;
		progressBar.TabIndex = 3;
		// 
		// btnPurge
		// 
		btnPurge.Enabled = false;
		btnPurge.Location = new Point(12, 85);
		btnPurge.Name = "btnPurge";
		btnPurge.Size = new Size(260, 23);
		btnPurge.TabIndex = 4;
		btnPurge.Text = "Purge";
		btnPurge.UseVisualStyleBackColor = true;
		btnPurge.Click += btnPurge_Click;
		// 
		// folderDialog
		// 
		folderDialog.Description = "Select your Helldivers 2 installation location";
		// 
		// MainForm
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(284, 120);
		Controls.Add(btnPurge);
		Controls.Add(progressBar);
		Controls.Add(btnBrowse);
		Controls.Add(label1);
		Controls.Add(txtGameDir);
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		Name = "MainForm";
		Text = "Purger";
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private TextBox txtGameDir;
	private Label label1;
	private Button btnBrowse;
	private ProgressBar progressBar;
	private Button btnPurge;
	private FolderBrowserDialog folderDialog;
}