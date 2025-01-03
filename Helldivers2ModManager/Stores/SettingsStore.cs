using Helldivers2ModManager.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Helldivers2ModManager.Stores;

internal sealed class SettingsStore
{
	public string TempDirectory { get; set; }

	public string GameDirectory { get; set; }

	public string StorageDirectory { get; set; }

	public LogLevel LogLevel { get; set; }

	public float Opacity { get; set; }

	public ObservableCollection<string> SkipList { get; set; }

	public bool CaseSensitiveSearch { get; set; }

	public event EventHandler? SettingsChanged;

	private static readonly FileInfo s_settingFile = new("settings.json");

	public SettingsStore()
	{
		Load();
	}

	[MemberNotNull(nameof(TempDirectory), nameof(GameDirectory), nameof(StorageDirectory), nameof(LogLevel), nameof(Opacity), nameof(SkipList))]
	public void Load()
	{
		Reset();

		if (s_settingFile.Exists)
		{
			byte[] data = File.ReadAllBytes(s_settingFile.FullName);
			var reader = new Utf8JsonReader(data, new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
			try
			{
				if (JsonDocument.TryParseValue(ref reader, out var doc))
				{
					var root = doc.RootElement;
					if (root.TryGetProperty(nameof(TempDirectory), out var prop))
						TempDirectory = prop.GetString()!;
					if (root.TryGetProperty(nameof(GameDirectory), out prop))
						GameDirectory = prop.GetString()!;
					if (root.TryGetProperty(nameof(StorageDirectory), out prop))
						StorageDirectory = prop.GetString()!;
					if (root.TryGetProperty(nameof(LogLevel), out prop))
						LogLevel = (LogLevel)prop.GetInt32();
					if (root.TryGetProperty(nameof(Opacity), out prop))
						Opacity = prop.GetSingle();
					var arr = root.OptionalStringArrayProp(nameof(SkipList));
					if (arr is not null)
						foreach (var str in arr)
							SkipList.Add(str);
					if (root.TryGetProperty(nameof(CaseSensitiveSearch), out prop))
						CaseSensitiveSearch = prop.GetBoolean();
				}
			}
			catch(JsonException)
			{ }
		}
	}

	[MemberNotNull(nameof(TempDirectory), nameof(GameDirectory), nameof(StorageDirectory), nameof(LogLevel), nameof(Opacity), nameof(SkipList))]
	public void Reset()
	{
		TempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "Helldivers2ModManager");
		GameDirectory = string.Empty;
		StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Helldivers2ModManager");
		LogLevel = LogLevel.Warning;
		Opacity = 0.8f;
		SkipList = [];
		CaseSensitiveSearch = false;
	}

	public void Save()
	{
		using var stream = s_settingFile.Exists ? s_settingFile.OpenWrite() : s_settingFile.Create();
		using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

		writer.WriteStartObject();

		writer.WriteString(nameof(TempDirectory), TempDirectory);
		writer.WriteString(nameof(GameDirectory), GameDirectory);
		writer.WriteString(nameof(StorageDirectory), StorageDirectory);
		writer.WriteNumber(nameof(LogLevel), (int)LogLevel);
		writer.WriteNumber(nameof(Opacity), Opacity);
		writer.WriteBoolean(nameof(CaseSensitiveSearch), CaseSensitiveSearch);

		writer.WriteStartArray(nameof(SkipList));
		foreach (var item in SkipList)
			writer.WriteStringValue(item);
		writer.WriteEndArray();

		writer.WriteEndObject();
		
		writer.Flush();

		SettingsChanged?.Invoke(this, EventArgs.Empty);
	}
}
