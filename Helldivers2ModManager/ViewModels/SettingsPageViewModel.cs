using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class SettingsPageViewModel(NavigationStore navStore, SettingsStore settingsStore) : PageViewModelBase
{
	public override string Title => "Settings";

	public string GameDir
	{
		get => _settingsStore.GameDirectory;
		set
		{
			OnPropertyChanging();
			_settingsStore.GameDirectory = value;
			OnPropertyChanged();
		}
	}

	public string TempDir
	{
		get => _settingsStore.TempDirectory;
		set
		{
			OnPropertyChanging();
			_settingsStore.TempDirectory = value;
			OnPropertyChanged();
		}
	}

	public string StorageDir
	{
		get => _settingsStore.StorageDirectory;
		set
		{
			OnPropertyChanging();
			_settingsStore.StorageDirectory = value;
			OnPropertyChanged();

			_storageDirChanged = true;
			ShowInfo("Storage directory changed. The application needs to be restarted and will quit once you hit \"OK\".");
		}
	}

	private readonly NavigationStore _navStore = navStore;
	private readonly SettingsStore _settingsStore = settingsStore;
	[ObservableProperty]
	private Visibility _messageVisibility = Visibility.Hidden;
	[ObservableProperty]
	private string _messageTitle = string.Empty;
	[ObservableProperty]
	private string _messageText = string.Empty;
	private bool _storageDirChanged = false;

	private bool ValidateSettings()
	{
		if (string.IsNullOrEmpty(GameDir))
		{
			ShowError("Game directory can not be left empty!");
			return false;
		}

		if (string.IsNullOrEmpty(StorageDir))
		{
			ShowError("Storage directory can not be left empty!");
			return false;
		}

		if (string.IsNullOrEmpty(TempDir))
		{
			ShowError("Temporary directory can not be left empty!");
			return false;
		}

		return true;
	}

	private void ShowInfo(string message)
	{
		MessageVisibility = Visibility.Visible;
		MessageTitle = "Info";
		MessageText = message;
	}

	private void ShowError(string message)
	{
		MessageVisibility = Visibility.Visible;
		MessageTitle = "Error";
		MessageText = message;
	}

	[RelayCommand]
	void Ok()
	{
		if (!ValidateSettings())
			return;

		_settingsStore.Save();

		if (_storageDirChanged)
			Application.Current.Shutdown();
		else
			_navStore.Navigate<DashboardPageViewModel>();
	}

	[RelayCommand]
	void Reset()
	{
		_settingsStore.Reset();
		OnPropertyChanged(nameof(GameDir));
		OnPropertyChanged(nameof(TempDir));
		OnPropertyChanged(nameof(StorageDir));
	}

	[RelayCommand]
	void BrowseGame()
	{
		var dialog = new OpenFolderDialog
        {
			Multiselect = false,
			Title = "Please select you Helldivers 2 folder..."
		};

		if (dialog.ShowDialog() ?? false)
		{
			var newDir = new DirectoryInfo(dialog.FolderName);

			if (newDir.Parent is DirectoryInfo { Name: "Helldivers 2" })
			{
				newDir = newDir.Parent;
			}

            if (newDir is not DirectoryInfo { Name: "Helldivers 2" })
			{ 
                ShowError("The selected Helldivers 2 folder does not reside in a valid directory!");
                return;
            }

            var subDirs = newDir.EnumerateDirectories();
			if (!subDirs.Any(static dir => dir.Name == "data"))
			{
				ShowError("The selected Helldivers 2 root path does not contain a directory named \"data\"!");
				return;
			}
			if (!subDirs.Any(static dir => dir.Name == "tools"))
			{
				ShowError("The selected Helldivers 2 root path does not contain a directory named \"tools\"!");
				return;
			}
            if (!subDirs.Any(static dir => dir.Name == "bin"))
            {
                ShowError("The selected Helldivers 2 root path does not contain a directory named \"bin\"!");
                return;
            }

            GameDir = newDir.FullName;
		}
		else
		{
			ShowError("The selected path is not a valid Helldivers 2 root!");
		}
	}

	[RelayCommand]
	void BrowseStorage()
	{
		var dialog = new OpenFolderDialog
		{
			Multiselect = false,
			ValidateNames = true,
			Title = "Please select a folder where you want this manager to store its mods..."
		};

		if (dialog.ShowDialog() ?? false)
			StorageDir = dialog.FolderName;
	}

	[RelayCommand]
	void BrowseTemp()
	{
		var dialog = new OpenFolderDialog
		{
			Multiselect = false,
			ValidateNames = true,
			Title = "Please select a folder which you want this manager to use for temporary files..."
		};

		if (dialog.ShowDialog() ?? false)
			TempDir = dialog.FolderName;
	}

	[RelayCommand]
	void MessageOk()
	{
		MessageVisibility = Visibility.Hidden;
	}
}
