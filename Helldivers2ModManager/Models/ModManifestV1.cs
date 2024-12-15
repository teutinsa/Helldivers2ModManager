namespace Helldivers2ModManager.Models;

internal sealed class ModManifestV1
{
	public required Guid Guid { get; init; }

	public required string Name { get; init; }

	public required string Description { get; init; }

	public string? IconPath { get; init; }

	public IReadOnlyList<ModOption>? Options { get; init; }

	public NexusData? NexusData { get; init; }
}
