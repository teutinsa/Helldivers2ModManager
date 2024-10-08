using System.IO;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Stores;

internal sealed class SettingsStore
{
	public string TempDirectory { get; set; }

	public string GameDirectory { get; set; }

	public string StorageDirectory { get; set; }

	public LogLevel LogLevel { get; set; }

	private static readonly FileInfo s_settingFile = new("settings.json");

	public SettingsStore()
	{
		Load();
	}

	[MemberNotNull(nameof(TempDirectory), nameof(GameDirectory), nameof(StorageDirectory))]
	public void Load()
	{
		LogLevel = LogLevel.Warning;

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
				}
			}
			catch(JsonException)
			{ }
		}

		TempDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "Helldivers2ModManager");
		GameDirectory ??= string.Empty;
		StorageDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Helldivers2ModManager");
	}

	public void Reset()
	{
		TempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "Helldivers2ModManager");
		GameDirectory = string.Empty;
		StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Helldivers2ModManager");
		LogLevel = LogLevel.Warning;
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

		writer.WriteEndObject();
		
		writer.Flush();
	}
}
