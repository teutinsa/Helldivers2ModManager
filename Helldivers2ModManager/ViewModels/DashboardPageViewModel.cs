using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Exceptions;
using Helldivers2ModManager.Extensions;
using Helldivers2ModManager.Models;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class DashboardPageViewModel : PageViewModelBase
{
	private sealed class ListTupleJsonConverter : JsonConverter<ListTuple>
	{
		public override ListTuple Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			var guid = Guid.Parse(root.ExpectStringProp(nameof(ListTuple.Guid)));
			var enabled = root.ExpectBoolean(nameof(ListTuple.Enabled));
			bool[]? toggled;
			int[]? seleceted;

			if (root.TryGetProperty("Option", out var _))
			{
				toggled = [];
				seleceted = [root.ExpectInt32Prop("Option")];
			}
			else
			{
				toggled = root.ExpectBooleanArrayProp(nameof(ListTuple.Toggled));
				seleceted = root.ExpectIntArrayProp(nameof(ListTuple.Selected));
			}

			return new ListTuple
			{
				Guid = guid,
				Enabled = enabled,
				Toggled = toggled,
				Selected = seleceted
			};
		}

		public override void Write(Utf8JsonWriter writer, ListTuple value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString(nameof(ListTuple.Guid), value.Guid.ToString());
			writer.WriteBoolean(nameof(ListTuple.Enabled), value.Enabled);
			writer.WriteStartArray(nameof(ListTuple.Toggled));
			foreach (var x in value.Toggled)
				writer.WriteBooleanValue(x);
			writer.WriteEndArray();
			writer.WriteStartArray(nameof(ListTuple.Selected));
			foreach (var x in value.Selected)
				writer.WriteNumberValue(x);
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}

	private readonly struct ListTuple
	{
		public required Guid Guid { get; init; }

		public required bool Enabled { get; init; }

		public required bool[] Toggled { get; init; }

		public required int[] Selected { get; init; }
	}

	public override string Title => "Mods";

	public IReadOnlyList<ModViewModel> Mods { get; private set; }

	public bool IsSearchEmpty => string.IsNullOrEmpty(SearchText);

	private static readonly JsonSerializerOptions s_jsonOptions = new()
	{
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip
	};
	private static readonly ProcessStartInfo s_gameStartInfo = new("steam://run/553850") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_reportStartInfo = new("https://www.nexusmods.com/helldivers2/mods/109?tab=bugs") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_discordStartInfo = new("https://discord.gg/helldiversmodding") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_githubStartInfo = new("https://github.com/teutinsa/Helldivers2ModManager") { UseShellExecute = true };
	private readonly ILogger<DashboardPageViewModel> _logger;
	private readonly IServiceProvider _provider;
	private readonly Lazy<NavigationStore> _navStore;
	private readonly ModStore _modStore;
	private readonly SettingsStore _settingsStore;
	private readonly ObservableCollection<ModViewModel> _mods;
	[ObservableProperty]
	private Visibility _editVisibility = Visibility.Hidden;
	[ObservableProperty]
	private ModViewModel? _editMod;
	[ObservableProperty]
	private string _searchText = string.Empty;

	static DashboardPageViewModel()
	{
		s_jsonOptions.Converters.Add(new ListTupleJsonConverter());
	}

	public DashboardPageViewModel(ILogger<DashboardPageViewModel> logger, IServiceProvider provider, ModStore modStore, SettingsStore settingsStore)
	{
		_logger = logger;
		_provider = provider;
		_navStore = new(_provider.GetRequiredService<NavigationStore>);
		_modStore = modStore;
		_settingsStore = settingsStore;
		_mods = [];

		var enabledFile = new FileInfo(Path.Combine(_settingsStore.StorageDirectory, "enabled.json"));
		ListTuple[]? list = null;
		if (enabledFile.Exists)
		{
			using var stream = enabledFile.OpenRead();
			try
			{
				list = JsonSerializer.Deserialize<ListTuple[]>(stream, s_jsonOptions);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, "Unable to parse \"{}\"", enabledFile.FullName);
			}
		}
		list ??= [];

		_logger.LogInformation("Listing mods");
		foreach (var item in list)
		{
			var mod = _modStore.GetModByGuid(item.Guid);
			if (mod is null)
			{
				_logger.LogWarning("Mod {} not found, skipping", item.Guid);
				continue;
			}
			switch (mod.Manifest.Version)
			{
				case ModManifest.ManifestVersion.Legacy:
					_mods.Add(new ModViewModel(mod) { Enabled = item.Enabled, LegacySelectedOption = item.Selected[0] });
					break;

				case ModManifest.ManifestVersion.V1:
					{
						var vm = new ModViewModel(mod) { Enabled = item.Enabled };
						Array.Copy(item.Toggled, vm.Data.EnabledOptions, Math.Min(vm.Data.EnabledOptions.Length, item.Toggled.Length));
						Array.Copy(item.Selected, vm.Data.SelectedOptions, Math.Min(vm.Data.SelectedOptions.Length, item.Selected.Length));
						_mods.Add(vm);
					}
					break;
			}
		}
		_modStore.Mods.Where(m => !list.Where(itm => itm.Guid == m.Manifest.Guid).Any()).ToArray().ForEach(m => _mods.Add(new ModViewModel(m)));

		_modStore.ModAdded += (_, e) =>
		{
			_mods.Add(new ModViewModel(e.Mod) { Enabled = true });
			SearchText = string.Empty;
		};

		Mods = _mods;
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

		var enabledFile = new FileInfo(Path.Combine(_settingsStore.StorageDirectory, "enabled.json"));
		var list = _mods.Select(static m => m.Data.Manifest.Version switch
		{
			ModManifest.ManifestVersion.Legacy => new ListTuple
			{
				Guid = m.Guid,
				Enabled = m.Enabled,
				Toggled = [],
				Selected = [m.LegacySelectedOption]
			},
			ModManifest.ManifestVersion.V1 => new ListTuple
			{
				Guid = m.Guid,
				Enabled = m.Enabled,
				Toggled = m.Data.EnabledOptions,
				Selected = m.Data.SelectedOptions
			},
			_ => throw new InvalidOperationException()
		}).ToArray();
		if (!enabledFile.Directory!.Exists)
			enabledFile.Directory!.Create();
		using var stream = enabledFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
		await JsonSerializer.SerializeAsync(stream, list, s_jsonOptions);

		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
	}

	private void UpdateView()
	{
		if (IsSearchEmpty)
			Mods = _mods;
		else
			Mods = _mods.Where(vm =>
			{
				if (_settingsStore.CaseSensitiveSearch)
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
				await _modStore.TryAddModFromArchiveAsync(new FileInfo(dialog.FileName));
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
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = "Purging Mods",
			Message = "Please wait democratically."
		});

		await _modStore.PurgeAsync();

		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Deploy()
	{
		if (string.IsNullOrEmpty(_settingsStore.GameDirectory))
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
			await Task.Run(() => _modStore.DeployAsync(guids));

			await SaveEnabled();

			WeakReferenceMessenger.Default.Send(new MessageBoxInfoMessage()
			{
				Message = "Deployment successful."
			});
		}
		catch(DeployException ex)
		{
			_logger.LogWarning(ex, "Deployment failed");
			if (ex.Mod is ModData mod)
			{
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = $"Something is wrong with this mod: \"{mod.Manifest.Name}\"\n\n{ex.Message}\n\t{ex.InnerException?.Message}"
				});
			}
			else if (ex.FileTriplet.HasValue)
			{
				var triplet = ex.FileTriplet.Value;
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = $"{ex.Message}\n\t{ex.InnerException?.Message}"
				});
			}
			else
			{
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = $"{ex.Message}\n\t{ex.InnerException?.Message}"
				});
			}
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
		if (_modStore.Remove(modVm.Data))
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
