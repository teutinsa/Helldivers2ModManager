using System.IO;

namespace Helldivers2ModManager.Models;

internal sealed class ModData(DirectoryInfo dir, IModManifest manifest)
{
    public DirectoryInfo Directory { get; } = dir;

    public IModManifest Manifest { get; } = manifest;

    public bool Enabled { get; set; } = true;

    public bool[] EnabledOptions { get; private set; } = manifest.Version switch
    {
        ManifestVersion.Legacy => [],
        ManifestVersion.V1 => Enumerable.Repeat(true, ((V1ModManifest)manifest).Options is null ? 0 : ((V1ModManifest)manifest).Options!.Count).ToArray(),
        ManifestVersion.V2 => throw new NotSupportedException(),
        _ => throw new NotImplementedException()
    };

    public int[] SelectedOptions { get; private set; } = manifest.Version switch
    {
        ManifestVersion.Legacy => new int[1],
        ManifestVersion.V1 => new int[((V1ModManifest)manifest).Options is null ? 0 : ((V1ModManifest)manifest).Options!.Count],
        ManifestVersion.V2 => throw new NotSupportedException(),
        _ => throw new NotImplementedException()
    };

    public void ApplyData(in EnabledData data)
    {
        Enabled = data.Enabled;
        EnabledOptions = data.Toggled;
		SelectedOptions = data.Selected;
    }

    public EnabledData ToEnabledData()
    {
        return new EnabledData
        {
            Guid = Manifest.Guid,
            Enabled = Enabled,
            Toggled = EnabledOptions,
            Selected = SelectedOptions,
        };
    }
}