using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Models;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress;
using System.Collections.ObjectModel;
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

	public ObservableCollection<ModViewModel> Mods { get; }

	private static readonly JsonSerializerOptions s_jsonOptions = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
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
	[ObservableProperty]
	private Visibility _editVisibility = Visibility.Hidden;
	[ObservableProperty]
	private ModViewModel? _editMod;

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
		Mods = [];

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
					Mods.Add(new ModViewModel(mod) { Enabled = item.Enabled, LegacySelectedOption = item.Selected[0] });
					break;

				case ModManifest.ManifestVersion.V1:
					{
						var vm = new ModViewModel(mod) { Enabled = item.Enabled };
						Array.Copy(item.Toggled, vm.Data.EnabledOptions, Math.Min(vm.Data.EnabledOptions.Length, item.Toggled.Length));
						Array.Copy(item.Selected, vm.Data.SelectedOptions, Math.Min(vm.Data.SelectedOptions.Length, item.Selected.Length));
						Mods.Add(vm);
					}
					break;
			}
		}
		_modStore.Mods.Where(m => !list.Where(itm => itm.Guid == m.Manifest.Guid).Any()).ToArray().ForEach(m => Mods.Add(new ModViewModel(m)));

		_modStore.ModAdded += (_, e) => Mods.Add(new ModViewModel(e.Mod) { Enabled = true });
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

	[RelayCommand]
	void Settings()
	{
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

		var mods = Mods.Where(static vm => vm.Enabled).ToArray();
		var guids = mods.Select(static vm => vm.Guid).ToArray();

		try
		{
			await Task.Run(() => _modStore.DeployAsync(guids));

			var enabledFile = new FileInfo(Path.Combine(_settingsStore.StorageDirectory, "enabled.json"));
			var list = Mods.Select(static m => m.Data.Manifest.Version switch
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
			using var stream = enabledFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
			await JsonSerializer.SerializeAsync(stream, list, s_jsonOptions);

			WeakReferenceMessenger.Default.Send(new MessageBoxInfoMessage()
			{
				Message = "Deployment successful."
			});
		}
		catch(Exception ex)
		{
			_logger.LogWarning(ex, "Deployment failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = ex.Message
			});
		}
	}

	[RelayCommand]
	void MoveUp(ModViewModel modVm)
	{
		var index = Mods.IndexOf(modVm);
		if (index <= 0)
			return;
		Mods.Move(index, index - 1);

	}

	[RelayCommand]
	void MoveDown(ModViewModel modVm)
	{
		var index = Mods.IndexOf(modVm);
		if (index >= Mods.Count - 1)
			return;
		Mods.Move(index, index + 1);
	}

	[RelayCommand]
	void Remove(ModViewModel modVm)
	{
		if (_modStore.Remove(modVm.Data))
			Mods.Remove(modVm);
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
}
