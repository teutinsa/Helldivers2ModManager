using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Models;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class DashboardPageViewModel : PageViewModelBase
{
	private sealed class ListTupleJsonConverter : JsonConverter<ListTuple>
	{
		public override ListTuple Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			return new ListTuple(Guid.Parse(root.GetProperty(nameof(ListTuple.Guid)).GetString()!), root.GetProperty(nameof(ListTuple.Option)).GetInt32());
		}

		public override void Write(Utf8JsonWriter writer, ListTuple value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString(nameof(ListTuple.Guid), value.Guid.ToString());
			writer.WriteNumber(nameof(ListTuple.Option), value.Option);
			writer.WriteEndObject();
		}
	}

	private readonly struct ListTuple(Guid guid, int option)
	{
		public Guid Guid { get; } = guid;

		public int Option { get; } = option;
	}

	public override string Title => "Mods";

	public ObservableCollection<ModViewModel> Mods { get; }

	private static readonly JsonSerializerOptions s_jsonOptions = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
		ReadCommentHandling = JsonCommentHandling.Skip
	};
	private static readonly ProcessStartInfo s_gameStartInfo = new("steam://run/553850")
	{
		UseShellExecute = true
	};
	private readonly ILogger<DashboardPageViewModel> _logger;
	private readonly IServiceProvider _provider;
	private readonly Lazy<NavigationStore> _navStore;
	private readonly ModStore _modStore;
	private readonly SettingsStore _settingsStore;
	[ObservableProperty]
	private Visibility _messageVisibility = Visibility.Hidden;
	[ObservableProperty]
	private string _messageTitle = string.Empty;
	[ObservableProperty]
	private string _messageText = string.Empty;
	[ObservableProperty]
	private Visibility _messageOkVisibility = Visibility.Collapsed;
	[ObservableProperty]
	private Visibility _messageProgressVisibility = Visibility.Collapsed;
	[ObservableProperty]
	private Color _messageColor = Colors.Yellow;

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
			catch(JsonException ex)
			{
				_logger.LogError(ex, "Unable to pare \"{}\"", enabledFile.FullName);
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
			Mods.Add(new ModViewModel(mod) { Enabled = true, SelectedOption = item.Option });
		}
		_modStore.Mods.Where(m => !list.Where(itm => itm.Guid == m.Manifest.Guid).Any()).ToArray().ForEach(m => Mods.Add(new ModViewModel(m)));

		_modStore.ModAdded += (_, e) => Mods.Add(new ModViewModel(e.Mod) { Enabled = true });
	}

	private void ShowError(string message)
	{
		MessageTitle = "Error";
		MessageText = message;
		MessageColor = Colors.Red;
		MessageOkVisibility = Visibility.Visible;
		MessageProgressVisibility = Visibility.Collapsed;
		MessageVisibility = Visibility.Visible;
	}

	private void ShowInfo(string message)
	{
		MessageTitle = "Info";
		MessageText = message;
		MessageColor = Colors.Yellow;
		MessageOkVisibility = Visibility.Visible;
		MessageProgressVisibility = Visibility.Collapsed;
		MessageVisibility = Visibility.Visible;
	}

	private void ShowProgress(string actionName)
	{
		MessageTitle = actionName;
		MessageText = "Please wait democratically.";
		MessageColor = Colors.Yellow;
		MessageOkVisibility = Visibility.Collapsed;
		MessageProgressVisibility = Visibility.Visible;
		MessageVisibility = Visibility.Visible;
	}

	private void HideMessage()
	{
		MessageVisibility = Visibility.Hidden;
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
			ShowProgress("Adding mod");
			if (await _modStore.TryAddModFromArchiveAsync(new FileInfo(dialog.FileName)))
			{
				HideMessage();
			}
			else
			{
				ShowError("Unable to add mod!");
			}
		}
	}

	[RelayCommand]
	void Browse()
	{

	}

	[RelayCommand]
	void Create()
	{

	}

	[RelayCommand]
	void Settings()
	{
		_navStore.Value.Navigate<SettingsPageViewModel>();
	}

	[RelayCommand]
	async Task Purge()
	{
		ShowProgress("Purging Mods");
		await _modStore.PurgeAsync();
		HideMessage();
	}

	[RelayCommand]
	async Task Deploy()
	{
		if (string.IsNullOrEmpty(_settingsStore.GameDirectory))
		{
			ShowError("Unable to deploy! Helldivers 2 Path not set. Please go to settings.");
			return;
		}

		ShowProgress("Deploying Mods");
		await Task.Delay(10);
		var mods = Mods.Where(static vm => vm.Enabled).ToArray();
		var guids = mods.Select(static vm => vm.Guid).ToArray();
		if (await _modStore.DeployAsync(guids))
		{
			var enabledFile = new FileInfo(Path.Combine(_settingsStore.StorageDirectory, "enabled.json"));
			var list = guids.Zip(mods.Select(static m => m.SelectedOption)).Select(static tpl => new ListTuple(tpl.First, tpl.Second)).ToArray();
			using var stream = enabledFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
			await JsonSerializer.SerializeAsync(stream, list);

			ShowInfo("Deployment successful.");
		}
		else
			ShowError("Deployment failed!");
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
	void MessageOk()
	{
		HideMessage();
	}

	[RelayCommand]
	void Run()
	{
		Process.Start(s_gameStartInfo);
	}
}
