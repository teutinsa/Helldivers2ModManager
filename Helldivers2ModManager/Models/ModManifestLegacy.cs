namespace Helldivers2ModManager.Models;

internal sealed class ModManifestLegacy
{
	public required Guid Guid { get; init; }

	public required string Name { get; init; }

	public required string Description { get; init; }

	public string? IconPath { get; init; }

	public IReadOnlyList<string>? Options { get; init; }
}