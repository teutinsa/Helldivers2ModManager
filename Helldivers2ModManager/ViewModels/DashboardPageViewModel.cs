using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Models;
using Helldivers2ModManager.Services;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using MessageBox = Helldivers2ModManager.Components.MessageBox;

namespace Helldivers2ModManager.ViewModels;

[RegisterService(ServiceLifetime.Transient)]
internal sealed partial class DashboardPageViewModel : PageViewModelBase
{
	public override string Title => "Mods";

	public IReadOnlyList<ModViewModel> Mods { get; private set; }

	public bool IsSearchEmpty => string.IsNullOrEmpty(SearchText);
	
	private static readonly ProcessStartInfo s_gameStartInfo = new("steam://run/553850") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_reportStartInfo = new("https://teutinsa.github.io/hd2mm-site/help/bug_reporting.html") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_discordStartInfo = new("https://discord.gg/helldiversmodding") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_githubStartInfo = new("https://github.com/teutinsa/Helldivers2ModManager") { UseShellExecute = true };
	private readonly ILogger<DashboardPageViewModel> _logger;
	private readonly Lazy<NavigationStore> _navStore;
	private readonly ModService _modService;
	private readonly SettingsService _settingsService;
	private readonly ProfileService _profileService;
	private ObservableCollection<ModViewModel> _mods;
	[ObservableProperty]
	private Visibility _editVisibility = Visibility.Hidden;
	[ObservableProperty]
	private ModViewModel? _editMod;
	[ObservableProperty]
	private string _searchText = string.Empty;
	[ObservableProperty]
	private bool _initialized = false;

	public DashboardPageViewModel(ILogger<DashboardPageViewModel> logger, IServiceProvider provider, SettingsService settingsService, ModService modService, ProfileService profileService)
	{
		_logger = logger;
		_navStore = new(provider.GetRequiredService<NavigationStore>);
		_settingsService = settingsService;
		_modService = modService;
		_profileService = profileService;
		_mods = [];

		Mods = _mods;

		if (MessageBox.IsRegistered)
			_ = Init();
		else
			MessageBox.Registered += (_, _) => _ = Init();
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

		await _profileService.SaveAsync(_settingsService, _mods.Select(static vm => vm.Data));

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
				return vm.Name.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);
			}).ToArray();
		OnPropertyChanged(nameof(Mods));
	}

	private async Task Init()
	{
		_logger.LogInformation("Initializing dashboard...");

		_logger.LogInformation("Loading settings...");
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
		{
			Title = "Loading settings",
			Message = "Please wait democratically.",
		});
		try
		{
			if (!await _settingsService.InitAsync(true))
				_settingsService.InitDefault(true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading settings failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxConfirmMessage
			{
				Title = $"Loading settings failed!",
				Message = "Go to settings now?",
				Confirm = _navStore.Value.Navigate<SettingsPageViewModel>,
			});
			return;
		}
		_logger.LogInformation("Settings loaded successfully");
		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());

		_logger.LogInformation("Validating settings");
		if (!_settingsService.Validate())
		{
			_logger.LogError("Settings invalid");
			WeakReferenceMessenger.Default.Send(new MessageBoxConfirmMessage
			{
				Title = $"Settings invalid!",
				Message = "Go to settings now?",
				Confirm = _navStore.Value.Navigate<SettingsPageViewModel>,
			});
			return;
		}
		_logger.LogInformation("Settings valid");
		
		_logger.LogInformation("Loading mods...");
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
		{
			Title = "Loading mods",
			Message = "Please wait democratically.",
		});
		ModProblem[] problems;
		try
		{
			problems = await Task.Run(() => _modService.Init(_settingsService));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading mods failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = $"Loading mods failed!\n\n{ex}",
			});
			return;
		}
		_modService.ModAdded += ModService_ModAdded;
		_modService.ModRemoved += ModService_ModRemoved;
		if (problems.Length != 0)
			_logger.LogWarning("Loaded mods with {} problems", problems.Length);
		else
			_logger.LogInformation("Mods loaded successfully");
		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());

		_logger.LogInformation("Loading profile...");
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
		{
			Title = "Loading profile",
			Message = "Please wait democratically.",
		});
		IReadOnlyList<ModData>? result;
		try
		{
			result = await _profileService.LoadAsync(_settingsService, _modService);
			result ??= _profileService.InitDefault(_modService);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading profile failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = $"Loading profile failed!\n\n{ex}",
			});
			return;
		}
		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
		_logger.LogInformation("Profile loaded successfully");

		_logger.LogInformation("Applying profile");
		_mods = new(result.Select(static data => new ModViewModel(data)).ToList());
		UpdateView();

		if (problems.Length > 0)
			ShowProblems(problems, "Problems with loading mods:");
		Initialized = true;
		_logger.LogInformation("Initialization successful");

#if DEBUG && FALSE
		ShowProblems(Enum.GetValues<ModProblemKind>().Select(static k => new ModProblem { Directory = new DirectoryInfo(@"C:\ModStorage\Test"), Kind = k }), "Problem test:", true);
#endif
	}

	private void ShowProblems(IEnumerable<ModProblem> problems, string prefix, bool error = false)
	{
		var sb = new StringBuilder();
		sb.AppendLine(prefix);

		var errors = problems.Where(static p => p.IsError).ToArray();
		if (errors.Length != 0)
		{
			sb.AppendLine("Errors:");
			foreach (var e in errors)
			{
				sb.Append("\t - \"");
				sb.Append(e.Directory.FullName);
				sb.AppendLine("\"");

				sb.Append("\t\t");
				string desc = e.Kind switch
				{
					ModProblemKind.CantParseManifest => "Can't parse manifest!",
					ModProblemKind.UnknownManifestVersion => "Unknown manifest version!",
					ModProblemKind.OutOfSupportManifest => $"Unsupported manifest version! Please update.\n\t\tManager version {App.Version} does not support this version of the manifest.",
					ModProblemKind.Duplicate => "A mod with the same GUID was already added!",
					ModProblemKind.InvalidPath => e.ExtraData is not null
						? $"The include path \"{e.ExtraData}\" is invalid!"
						: "A include path is invalid!",
					_ => throw new NotImplementedException()
				};
				sb.AppendLine(desc);
			}
		}

		var warnings = problems.Where(static p => !p.IsError).ToArray();
		if (warnings.Length != 0)
		{
			sb.AppendLine("Warnings:");
			foreach (var w in warnings)
			{
				sb.Append("\t - \"");
				sb.Append(w.Directory.FullName);
				sb.AppendLine("\"");

				sb.Append("\t\t");
				string desc = w.Kind switch
				{
					ModProblemKind.NoManifestFound => error
						? "No manifest found in directory!"
						: "No manifest found in directory!\n\t\t\tAction: Deleting",
					ModProblemKind.EmptyOptions => "Manifest contains empty options! This mod will likely do nothing.",
					ModProblemKind.EmptySubOptions => "Manifest contains empty sub-options! This mod will likely not work as expected.",
					ModProblemKind.EmptyIncludes => "Manifest contains empty include lists! This mod my not do anything.",
					_ => throw new NotImplementedException()
				};
				sb.AppendLine(desc);
			}
		}

		if (error)
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = sb.ToString(),
			});
		else
			WeakReferenceMessenger.Default.Send(new MessageBoxWarningMessage
			{
				Message = sb.ToString(),
			});
	}

	private void ModService_ModAdded(ModData mod)
	{
		_mods.Add(new ModViewModel(mod));
		SearchText = string.Empty;
	}

	private void ModService_ModRemoved(ModData mod)
	{
		var vm = _mods.First((vm) => vm.Data == mod);
		if (vm is not null)
			_mods.Remove(vm);
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
			WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
			{
				Title = "Adding Mod",
				Message = "Please wait democratically."
			});
			try
			{
				var problems = await _modService.TryAddModFromArchiveAsync(new FileInfo(dialog.FileName));
				if (problems.Length > 0)
				{
					var error = problems.Any(static p => p.IsError);
					var prefix = error
						? "Mod adding failed due to problems:"
						: "Mod added with warnings:";
					ShowProblems(problems, prefix, error);
				}
				else
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
		Process.Start(s_reportStartInfo);
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
			await SaveEnabled();

			await _modService.DeployAsync(guids);

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
	async Task Remove(ModViewModel modVm)
	{
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = "Removing Mod",
			Message = "Please wait democratically."
		});

		try
		{
			await _modService.RemoveAsync(modVm.Data);

			WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unknown mod removal error");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = ex.Message
			});
		}
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
