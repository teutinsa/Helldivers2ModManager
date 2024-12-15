namespace Helldivers2ModManager.Models;

internal sealed class ModSubOption
{
	public required string Name { get; init; }

	public required string Description { get; init; }

	public required string[] Include { get; init; }

	public string? Image { get; init; }
}
