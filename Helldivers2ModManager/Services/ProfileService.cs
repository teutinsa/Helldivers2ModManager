using Helldivers2ModManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Transient)]
internal sealed class ProfileService
{
	private readonly ILogger<ProfileService> _logger;

	public ProfileService(ILogger<ProfileService> logger)
	{
		_logger = logger;
	}

	public async Task<IReadOnlyList<ModData>?> LoadAsync(SettingsService settingsService, ModService modService)
	{
		var enabledFile = new FileInfo(Path.Combine(settingsService.StorageDirectory, "enabled.json"));

		if (!enabledFile.Exists)
		{
			_logger.LogInformation("\"enabled.json\" not found terminating initialization");
			return null;
		}

		_logger.LogInformation("Parsing \"enabled.json\"");
		using var stream = enabledFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
		var doc = await JsonDocument.ParseAsync(stream);
		var root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Array)
			throw new SerializationException("Expected document root to be of type `array`!");

		var len = root.GetArrayLength();
		_logger.LogInformation("Found {} potential entries", len);

		var mods = new List<ModData>(modService.Mods.Count);
		foreach (var elm in root.EnumerateArray())
		{
			if (elm.ValueKind != JsonValueKind.Object)
				throw new SerializationException("Expected array element to be of type `object`!");

			var data = EnabledData.Deserialize(elm);
			_logger.LogDebug("Processing {}", data);

			var mod = modService.GetModByGuid(data.Guid);
			if (mod is null)
			{
				_logger.LogWarning("{} has no corresponding mod, skipping", data.Guid);
				continue;
			}

			mod.ApplyData(data);

			mods.Add(mod);
		}

		var remainder = modService.Mods.Count - len;
		if (remainder > 0)
		{
			_logger.LogInformation("{} unaccounted for, adding with default configurations", remainder);
			foreach (var elm in modService.Mods)
				if (!mods.Contains(elm))
					mods.Add(elm);
		}

		return mods.ToArray();
	}

	public IReadOnlyList<ModData> InitDefault(ModService modService)
	{
		_logger.LogInformation("Loading profile default");
		return modService.Mods;
	}

	public async Task SaveAsync(SettingsService settingsService, IEnumerable<ModData> mods)
	{
		_logger.LogInformation("Saving profile");

		var stream = File.Open(Path.Combine(settingsService.StorageDirectory, "enabled.json"), FileMode.Create, FileAccess.Write, FileShare.Read);
		var writer = new Utf8JsonWriter(stream);

		writer.WriteStartArray();
		foreach (var elm in mods.Select(static m => m.ToEnabledData()))
			elm.Serialize(writer);
		writer.WriteEndArray();

		await writer.DisposeAsync();
		await stream.DisposeAsync();

		_logger.LogInformation("Profile saved");
	}
}
