using System.IO;
using System.Text.Json;
using Helldivers2ModManager.Models;

namespace Helldivers2ModManager.Services.Manifest;

internal sealed class ModManifestV1Service : IModManifestService
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
				"SubOption": [
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
	}

	public Task<object?> InferrFromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	public Task ToFileAsync(object manifest, FileInfo dest, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}
