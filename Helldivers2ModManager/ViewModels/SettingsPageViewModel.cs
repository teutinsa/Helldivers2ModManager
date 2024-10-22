using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class SettingsPageViewModel(ILogger<SettingsPageViewModel> logger, NavigationStore navStore, SettingsStore settingsStore) : PageViewModelBase
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
			WeakReferenceMessenger.Default.Send(new MessageBoxInfoMessage()
			{
				Message = "Storage directory changed. The application needs to be restarted and will quit once you hit \"OK\"."
			});
		}
	}

	public LogLevel LogLevel
	{
		get => _settingsStore.LogLevel;
		set
		{
			OnPropertyChanging();
			_settingsStore.LogLevel = value;
			OnPropertyChanged();
		}
	}

	public float Opacity
	{
		get => _settingsStore.Opacity;
		set
		{
			OnPropertyChanging();
			_settingsStore.Opacity = value;
			OnPropertyChanged();
		}
	}

	private readonly ILogger<SettingsPageViewModel> _logger = logger;
	private readonly NavigationStore _navStore = navStore;
	private readonly SettingsStore _settingsStore = settingsStore;
	private bool _storageDirChanged = false;

	private bool ValidateSettings()
	{
		if (string.IsNullOrEmpty(GameDir))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = "Game directory can not be left empty!"
			});
			return false;
		}

		if (string.IsNullOrEmpty(StorageDir))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = "Storage directory can not be left empty!"
			});
			return false;
		}

		if (string.IsNullOrEmpty(TempDir))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = "Temporary directory can not be left empty!"
			});
			return false;
		}

		return true;
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
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = "The selected Helldivers 2 folder does not reside in a valid directory!"
				});
				return;
			}

			var subDirs = newDir.EnumerateDirectories();
			if (!subDirs.Any(static dir => dir.Name == "data"))
			{
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = "The selected Helldivers 2 root path does not contain a directory named \"data\"!"
				});
				return;
			}
			if (!subDirs.Any(static dir => dir.Name == "tools"))
			{
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = "The selected Helldivers 2 root path does not contain a directory named \"tools\"!"
				});
				return;
			}
			if (!subDirs.Any(static dir => dir.Name == "bin"))
			{
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = "The selected Helldivers 2 root path does not contain a directory named \"bin\"!"
				});
				return;
			}

			GameDir = newDir.FullName;
		}
		else
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = "The selected path is not a valid Helldivers 2 root!"
			});
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
	void HardPurge()
	{
		_logger.LogInformation("Hard purging patch files");
		
		var path = Path.Combine(_settingsStore.StorageDirectory, "installed.txt");
		if (File.Exists(path))
			File.Delete(path);

		var dataDir = new DirectoryInfo(Path.Combine(_settingsStore.GameDirectory, "data"));
		
		var files = dataDir.EnumerateFiles("*.patch_*").ToArray();
		_logger.LogInformation("Found {} patch files", files.Length);

		foreach (var file in files)
		{
			_logger.LogTrace("Deleting \"{}\"", file.Name);
			file.Delete();
		}

		_logger.LogInformation("Hard purge complete");
	}
}
