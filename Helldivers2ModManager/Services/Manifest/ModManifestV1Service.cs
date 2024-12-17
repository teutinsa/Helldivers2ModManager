using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using Helldivers2ModManager.Models;

namespace Helldivers2ModManager.Services.Manifest;

internal sealed class ModManifestV1Service(ModManifestLegacyService service) : IModManifestService
{
	/*
	{
		"Version": 1,
		"Guid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
		"Name": "Mod Name",
		"Description": "",
		"IconPath": "icon.png" | null,
		"Options": [
			{
				"Name": "Option Name"
				"Description": "",
				"Include": [
					"Base"
				] | null,
				"Image": "img.png" | null,
				"SubOptions": [
					{
						"Name": "Sub Option Name",
						"Description": "",
						"Include": [
							"Addition"
						],
						"Image": "img.png" | null
					}
				] | null
			}
		] | null,
		"NexusData": {
			"ModId": 109,
			"Version": "1.0.0"
		} | null
	}
	*/
	private readonly ModManifestLegacyService _service = service;

	public async Task<object?> FromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
	{
		var file = directory.GetFiles("manifest.json").Where(static f => f.Name == "manifest.json").FirstOrDefault();
		if (file is not null)
			return await FromFileAsync(file, cancellationToken);
		else
			return await InferrFromDirectoryAsync(directory, cancellationToken);
	}

	public async Task<object?> FromFileAsync(FileInfo file, CancellationToken cancellationToken = default)
	{
		using var stream = file.OpenRead();
		var doc = await JsonDocument.ParseAsync(stream, IModManifestService.DocOptions, cancellationToken);
		var root = doc.RootElement;

		if (root.TryGetProperty("Version", out var prop))
		{
			var guid = Guid.Parse(root.ExpectStringProp("Guid"));
			var name = root.ExpectStringProp("Name");
			var description = root.ExpectStringProp("Description");
			var iconPath = root.OptionalStringProp("IconPath");
			ModOption[]? options = null;
			NexusData? nexusData = null;

			var elms = root.OptionalObjectArrayProp("Options");
			if (elms is not null)
			{
				options = new ModOption[elms.Length];
				for (int i = 0; i < elms.Length; i++)
				{
					var optName = elms[i].ExpectStringProp("Name");
					var optDescription = elms[i].ExpectStringProp("Description");
					var optInclude = elms[i].OptionalStringArrayProp("Include");
					var optImage = elms[i].OptionalStringProp("Image");
					ModSubOption[]? subOptions = null;

					var subElms = elms[i].OptionalObjectArrayProp("SubOptions");
					if (subElms is not null)
					{
						subOptions = new ModSubOption[subElms.Length];
						for (int j = 0; j < subElms.Length; j++)
						{
							var subName = subElms[j].ExpectStringProp("Name");
							var subDescription = subElms[j].ExpectStringProp("Description");
							var subInclude = subElms[j].ExpectStringArrayProp("Include");
							var subImage = subElms[j].OptionalStringProp("Image");

							subOptions[j] = new ModSubOption
							{
								Name = subName,
								Description = subDescription,
								Include = subInclude,
								Image = subImage
							};
						}
					}

					options[i] = new ModOption
					{
						Name = optName,
						Description = optDescription,
						Include = optInclude,
						Image = optImage,
						SubOptions = subOptions
					};
				}
			}

			if (root.OptionalObjectProp("NexusData") is JsonElement elm)
			{
				var modId = elm.ExpectUInt32Prop("ModId");
				var version = elm.ExpectStringProp("Version");

				nexusData = new NexusData
				{
					ModId = modId,
					Version = Version.Parse(version.TrimStart('v', 'V'))
				};
			}

			return new ModManifestV1
			{
				Guid = guid,
				Name = name,
				Description = description,
				IconPath = iconPath,
				Options = options,
				NexusData = nexusData
			};
		}
		else
			return await _service.FromFileAsync(file, cancellationToken);
	}

	public Task<object?> InferrFromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
	{
		return _service.InferrFromDirectoryAsync(directory, cancellationToken);
	}

	public async Task ToFileAsync(object manifest, FileInfo dest, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		if (manifest is ModManifestLegacy)
		{
			await _service.ToFileAsync(manifest, dest, cancellationToken);
			return;
		}

		var man = manifest as ModManifestV1 ?? throw new SerializationException($"This service can only serialize `{typeof(ModManifestV1)}`");
		var stream = dest.Create();
		var writer = new Utf8JsonWriter(stream);

		try
		{
			writer.WriteStartObject();
			writer.WriteNumber("Version", 1);
			writer.WriteString("Guid", man.Guid);
			writer.WriteString("Name", man.Name);
			writer.WriteString("Description", man.Description);
			if (man.IconPath is not null)
				writer.WriteString("IconPath", man.IconPath);
			if (man.Options is not null)
			{
				writer.WriteStartArray("Options");
				foreach (var item in man.Options)
				{
					writer.WriteStartObject();
					writer.WriteString("Name", item.Name);
					writer.WriteString("Description", item.Description);
					if (item.Include is not null)
					{
						writer.WriteStartArray("Include");
						foreach (var inc in item.Include)
							writer.WriteStringValue(inc);
						writer.WriteEndArray();
					}
					if (item.Image is not null)
						writer.WriteString("Image", item.Image);
					if (item.SubOptions is not null)
					{
						writer.WriteStartArray("SubOptions");
						foreach (var sub in item.SubOptions)
						{
							writer.WriteStartObject();
							writer.WriteString("Name", sub.Name);
							writer.WriteString("Description", sub.Description);
							if (sub.Image is not null)
								writer.WriteString("Image", sub.Image);
							writer.WriteStartArray("Include");
							foreach (var inc in sub.Include)
								writer.WriteStringValue(inc);
							writer.WriteEndArray();
							writer.WriteEndObject();
						}
						writer.WriteEndArray();
					}
					writer.WriteEndObject();
				}
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
