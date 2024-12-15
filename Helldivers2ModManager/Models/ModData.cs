using System.IO;

namespace Helldivers2ModManager.Models;

internal sealed class ModData(DirectoryInfo dir, ModManifest manifest)
{
	public DirectoryInfo Directory { get; } = dir;

	public ModManifest Manifest { get; } = manifest;

	public bool Enabled { get; set; } = true;

	public bool[] EnabedOptions { get; } = manifest.Version switch
	{
		ModManifest.ManifestVersion.Legacy => new bool[1],
		ModManifest.ManifestVersion.V1 => new bool[manifest.V1.Options is null ? 0 : manifest.V1.Options.Count],
		ModManifest.ManifestVersion.Unknown => throw new NotSupportedException(),
		_ => throw new NotImplementedException()
	};

	public int[] SelectedOptions { get; } = manifest.Version switch
	{
		ModManifest.ManifestVersion.Legacy => new int[1],
		ModManifest.ManifestVersion.V1 => new int[manifest.V1.Options is null ? 0 : manifest.V1.Options.Count],
		ModManifest.ManifestVersion.Unknown => throw new NotSupportedException(),
		_ => throw new NotImplementedException()
	};
}
