using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;

namespace Helldivers2ModManager.ViewModels;

[RegisterService(ServiceLifetime.Transient)]
internal sealed partial class DashboardPageViewModel : PageViewModelBase
{
	public override string Title => "Mods";

	public IReadOnlyList<ModViewModel> Mods { get; private set; }

	public bool IsSearchEmpty => string.IsNullOrEmpty(SearchText);
	
	private static readonly ProcessStartInfo s_gameStartInfo = new("steam://run/553850") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_reportStartInfo = new("https://www.nexusmods.com/helldivers2/mods/109?tab=bugs") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_discordStartInfo = new("https://discord.gg/helldiversmodding") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_githubStartInfo = new("https://github.com/teutinsa/Helldivers2ModManager") { UseShellExecute = true };
	private readonly ILogger<DashboardPageViewModel> _logger;
	private readonly Lazy<NavigationStore> _navStore;
	private readonly ModService _modService;
	private readonly ProfileService _profileService;
	private readonly SettingsService _settingsService;
	private readonly ObservableCollection<ModViewModel> _mods;
	[ObservableProperty]
	private Visibility _editVisibility = Visibility.Hidden;
	[ObservableProperty]
	private ModViewModel? _editMod;
	[ObservableProperty]
	private string _searchText = string.Empty;

	public DashboardPageViewModel(ILogger<DashboardPageViewModel> logger, IServiceProvider provider, ModService modService, ProfileService profileService, SettingsService settingsService)
	{
		_logger = logger;
		_navStore = new(provider.GetRequiredService<NavigationStore>);
		_modService = modService;
		_profileService = profileService;
		_settingsService = settingsService;
		_mods = [];
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(SearchText))
		{
			OnPropertyChanged(nameof(IsSearchEmpty));
			ClearSearchCommand.NotifyCanExecuteChanged();
			UpdateView();
		}

		base.OnPropertyChanged(e);
	}

	private async Task SaveEnabled()
	{
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = "Saving mod configuration",
			Message = "Please wait democratically."
		});

		await _profileService.SaveCurrentAsync(_settingsService);

		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
	}

	private void UpdateView()
	{
		if (IsSearchEmpty)
			Mods = _mods;
		else
			Mods = _mods.Where(vm =>
			{
				if (_settingsService.CaseSensitiveSearch)
					return vm.Name.Contains(SearchText, StringComparison.InvariantCulture);
				else
					return vm.Name.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);
			}).ToArray();
		OnPropertyChanged(nameof(Mods));
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Add()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Download"),
			Filter = "Archive|*.rar;*.7z;*.zip;*.tar",
			Multiselect = false,
			Title = "Please select a mod archive to add..."
		};

		if (dialog.ShowDialog() ?? false)
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
			{
				Title = "Adding Mod",
				Message = "Please wait democratically."
			});
			try
			{
				await _modService.TryAddModFromArchiveAsync(new FileInfo(dialog.FileName));
				WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
			}
			catch(Exception ex)
			{
				_logger.LogWarning(ex, "Failed to add mod");
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = ex.Message
				});
			}
		}
	}

	[RelayCommand]
	void Browse()
	{
		throw new NotImplementedException();
	}

	[RelayCommand]
	void Create()
	{
		_navStore.Value.Navigate<CreatePageViewModel>();
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void ReportBug()
	{
		WeakReferenceMessenger.Default.Send(new MessageBoxConfirmMessage
		{
			Title = "How to report bugs",
			Message = """
			If you want to report a bug please include the latest log file.
				- If you report a crash or an error, describe what you did until the error occurred.
				- If you want to report a bug of a mod not deploying, specify your mod list and the mod that caused the error.
				- If you want to report a bug of a mod not working correctly, this is not the place for that.

			If your issue is more specific feel free to join the Modding Discord.

			Continue?
			""",
			Confirm = static () => Process.Start(s_reportStartInfo)
		});
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Settings()
	{
		await SaveEnabled();
		
		_navStore.Value.Navigate<SettingsPageViewModel>();
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Purge()
	{
		if (string.IsNullOrEmpty(_settingsService.GameDirectory))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = "Unable to purge! Helldivers 2 Path not set. Please go to settings."
			});
			return;
		}

		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = "Purging Mods",
			Message = "Please wait democratically."
		});

		await _modService.PurgeAsync();

		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Deploy()
	{
		if (string.IsNullOrEmpty(_settingsService.GameDirectory))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = "Unable to deploy! Helldivers 2 Path not set. Please go to settings."
			});
			return;
		}

		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = "Deploying Mods",
			Message = "Please wait democratically."
		});

		var mods = _mods.Where(static vm => vm.Enabled).ToArray();
		var guids = mods.Select(static vm => vm.Guid).ToArray();

		try
		{
			await Task.Run(() => _modService.DeployAsync(guids));

			await SaveEnabled();

			WeakReferenceMessenger.Default.Send(new MessageBoxInfoMessage()
			{
				Message = "Deployment successful."
			});
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Unknown deployment error");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = ex.Message
			});
		}
	}

	[RelayCommand]
	void MoveUp(ModViewModel modVm)
	{
		var index = _mods.IndexOf(modVm);
		if (index <= 0)
			return;
		_mods.Move(index, index - 1);
	}

	[RelayCommand]
	void MoveDown(ModViewModel modVm)
	{
		var index = _mods.IndexOf(modVm);
		if (index >= _mods.Count - 1)
			return;
		_mods.Move(index, index + 1);
	}

	[RelayCommand]
	void Remove(ModViewModel modVm)
	{
		if (_modService.Remove(modVm.Data))
			_mods.Remove(modVm);
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void Run()
	{
		Process.Start(s_gameStartInfo);
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void Github()
	{
		Process.Start(s_githubStartInfo);
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void Discord()
	{
		Process.Start(s_discordStartInfo);
	}

	[RelayCommand]
	void Edit(ModViewModel vm)
	{
		EditMod = vm;
		EditVisibility = Visibility.Visible;
	}

	[RelayCommand]
	void EditDone()
	{
		EditVisibility = Visibility.Hidden;
		EditMod = null;
	}

	bool CanClearSearch()
	{
		return !IsSearchEmpty;
	}

	[RelayCommand(CanExecute = nameof(CanClearSearch))]
	void ClearSearch()
	{
		SearchText = string.Empty;
	}
}
