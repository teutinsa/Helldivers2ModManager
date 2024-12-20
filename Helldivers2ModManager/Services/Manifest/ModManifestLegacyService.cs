using Helldivers2ModManager.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Helldivers2ModManager.Services.Manifest;

internal sealed class ModManifestLegacyService(ILogger<ModManifestLegacyService> logger) : IModManifestService
{
	/*
	{
		"Guid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
		"Name": "Mod Name",
		"Description": "",
		"IconPath": "icon.png" | null,
		"Options": [
			"Option 1",
			"Option 2"
		] | null
	}
	 */

	private readonly ILogger<ModManifestLegacyService> _logger = logger;

	public async Task<object?> FromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
	{
		var file = directory.GetFiles("manifest.json").Where(static f => f.Name == "manifest.json").FirstOrDefault();
		if (file is not null)
			return await FromFileAsync(file, cancellationToken);
		else
			return await InferFromDirectoryAsync(directory, cancellationToken);
	}

	public async Task<object?> FromFileAsync(FileInfo file, CancellationToken cancellationToken = default)
	{
		using var stream = file.OpenRead();
		var doc = await JsonDocument.ParseAsync(stream, IModManifestService.DocOptions, cancellationToken);
		var root = doc.RootElement;

		var guid = Guid.Parse(root.ExpectStringProp("Guid"));
		var name = root.ExpectStringProp("Name");
		var description = root.ExpectStringProp("Description");
		var iconPath = root.OptionalStringProp("IconPath");
		var options = root.OptionalStringArrayProp("Options");

		return new ModManifestLegacy
		{
			Guid = guid,
			Name = name,
			Description = description,
			IconPath = iconPath,
			Options = options
		};
	}

	public Task<object?> InferFromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return null!;

		var guid = Guid.NewGuid();
		var name = directory.Name;
		var description = "A locally imported mod.";
		string? iconPath = null;
		string[]? options = null;

		var files = directory.GetFiles().Where(static f => IModManifestService.ImageExtensions.Contains(f.Extension));
		if (files.FirstOrDefault(static f => f.Name.Contains("icon")) is FileInfo icon)
			iconPath = icon.FullName;
		else if (files.FirstOrDefault() is FileInfo file)
			iconPath = file.FullName;

		var directories = directory.GetDirectories();
		if (directories.Length > 0)
			options = directories.Select(static d => d.Name).ToArray();

		return Task.FromResult((object?)new ModManifestLegacy
		{
			Guid = guid,
			Name = name,
			Description = description,
			IconPath = iconPath,
			Options = options
		});
	}

	public async Task ToFileAsync(object manifest, FileInfo dest, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		var man = manifest as ModManifestLegacy ?? throw new SerializationException($"This service can only serialize `{typeof(ModManifestLegacy)}`");
		var stream = dest.Create();
		var writer = new Utf8JsonWriter(stream);

		try
		{
			writer.WriteStartObject();
			writer.WriteString("Guid", man.Guid);
			writer.WriteString("Name", man.Name);
			writer.WriteString("Description", man.Description);
			if (man.IconPath is not null)
				writer.WriteString("IconPath", man.IconPath);
			if (man.Options is not null)
			{
				writer.WriteStartArray("Options");
				foreach (var item in man.Options)
					writer.WriteStringValue(item);
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
		finally
		{
			await writer.DisposeAsync();
			await stream.DisposeAsync();
		}
	}
}
