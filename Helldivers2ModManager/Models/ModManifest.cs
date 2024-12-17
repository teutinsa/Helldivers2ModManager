namespace Helldivers2ModManager.Models;

internal sealed class ModManifest(object inner)
{
	public enum ManifestVersion
	{
		Unknown,
		Legacy,
		V1
	}

	public object Inner => _inner;

	public ManifestVersion Version => _inner switch
	{
		ModManifestLegacy _ => ManifestVersion.Legacy,
		ModManifestV1 _ => ManifestVersion.V1,
		_ => ManifestVersion.Unknown
	};

	public ModManifestLegacy Legacy => _inner as ModManifestLegacy ?? throw new InvalidOperationException($"Inner is not of type `{typeof(ModManifestLegacy)}`");

	public ModManifestV1 V1 => _inner as ModManifestV1 ?? throw new InvalidOperationException($"Inner is not of type `{typeof(ModManifestV1)}`");

	public Guid Guid => Version switch
	{
		ManifestVersion.Legacy => Legacy.Guid,
		ManifestVersion.V1 => V1.Guid,
		ManifestVersion.Unknown => throw new NotSupportedException(),
		_ => throw new NotImplementedException()
	};

	public string Name => Version switch
	{
		ManifestVersion.Legacy => Legacy.Name,
		ManifestVersion.V1 => V1.Name,
		ManifestVersion.Unknown => throw new NotSupportedException(),
		_ => throw new NotImplementedException()
	};

	public string Description => Version switch
	{
		ManifestVersion.Legacy => Legacy.Description,
		ManifestVersion.V1 => V1.Description,
		ManifestVersion.Unknown => throw new NotSupportedException(),
		_ => throw new NotImplementedException()
	};

	public string? IconPath => Version switch
	{
		ManifestVersion.Legacy => Legacy.IconPath,
		ManifestVersion.V1 => V1.IconPath,
		_ => null
	};

	private readonly object _inner = inner;
}
