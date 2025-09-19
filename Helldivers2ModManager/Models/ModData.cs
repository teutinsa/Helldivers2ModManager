using System.IO;

namespace Helldivers2ModManager.Models;

public sealed class ModData(DirectoryInfo dir, IModManifest manifest)
{
    public DirectoryInfo Directory { get; } = dir;

    public IModManifest Manifest { get; } = manifest;

    public bool Enabled { get; set; } = true;

    public bool[] EnabledOptions { get; } = manifest.Version switch
    {
        ManifestVersion.Legacy => [],
        ManifestVersion.V1 => Enumerable.Repeat(true, ((V1ModManifest)manifest).Options is null ? 0 : ((V1ModManifest)manifest).Options!.Count).ToArray(),
        ManifestVersion.V2 => throw new NotSupportedException(),
        _ => throw new NotImplementedException()
    };

    public int[] SelectedOptions { get; } = manifest.Version switch
    {
        ManifestVersion.Legacy => new int[1],
        ManifestVersion.V1 => new int[((V1ModManifest)manifest).Options is null ? 0 : ((V1ModManifest)manifest).Options!.Count],
        ManifestVersion.V2 => throw new NotSupportedException(),
        _ => throw new NotImplementedException()
    };
}