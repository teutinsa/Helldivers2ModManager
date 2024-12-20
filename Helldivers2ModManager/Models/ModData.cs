using System.IO;

namespace Helldivers2ModManager.Models;

internal sealed class ModData(DirectoryInfo dir, ModManifest manifest)
{
	public DirectoryInfo Directory { get; } = dir;

	public ModManifest Manifest { get; } = manifest;

	public bool Enabled { get; set; } = true;

	public bool[] EnabledOptions { get; } = manifest.Version switch
	{
		ModManifest.ManifestVersion.Legacy => [],
		ModManifest.ManifestVersion.V1 => Enumerable.Repeat(true, manifest.V1.Options is null ? 0 : manifest.V1.Options.Count).ToArray(),
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
