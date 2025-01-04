using System.Diagnostics.CodeAnalysis;

namespace Purger;

public partial class MainForm : Form
{
	public MainForm()
	{
		InitializeComponent();
	}

	private static bool ValdiatePath(string path, [NotNullWhen(false)] out string? err)
	{
		var dir = new DirectoryInfo(path);
		var dirs = dir.GetDirectories();

		var binDir = dirs.FirstOrDefault(static d => d.Name == "bin");
		if (binDir is null)
		{
			err = "The selected folder does not have a directory named \"bin\"!";
			return false;
		}

		if (!binDir.GetFiles().Any(static f => f.Name == "helldivers2.exe"))
		{
			err = "The selected folders \"bin\" folder does not contain a file called \"helldivers2.exe\"!";
			return false;
		}

		if (!dirs.Any(static d => d.Name == "data"))
		{
			err = "The selected folder does not have a directory named \"bin\"!";
			return false;
		}

		if (!dirs.Any(static d => d.Name == "tools"))
		{
			err = "The selected folder does not have a directory named \"tools\"!";
			return false;
		}

		err = null;
		return true;
	}

	private void SafeInvoke(Action action)
	{
		if (InvokeRequired)
			Invoke(action);
	}

	async void btnPurge_Click(object sender, EventArgs e)
	{
		btnBrowse.Enabled = false;
		btnPurge.Enabled = false;
		progressBar.Value = 0;

		var count = await Task.Run(() =>
		{
			var dir = new DirectoryInfo(Path.Combine(txtGameDir.Text, "data"));
			var files = dir.EnumerateFiles("*.patch_*").ToArray();

			SafeInvoke(() => progressBar.Maximum = files.Length);

			foreach (var f in files)
			{
				f.Delete();
				SafeInvoke(() => progressBar.Value++);
			}

			return files.Length;
		});

		MessageBox.Show(this, $"Deleted {count} files!\nIt's recommended to verify your game files now.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

		btnBrowse.Enabled = true;
		btnPurge.Enabled = true;
	}

	void btnBrowse_Click(object sender, EventArgs e)
	{
		if (folderDialog.ShowDialog() != DialogResult.OK)
			return;

		if (!ValdiatePath(folderDialog.SelectedPath, out var err))
		{
			MessageBox.Show(this, err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		txtGameDir.Text = folderDialog.SelectedPath;
		btnPurge.Enabled = true;
	}
}
