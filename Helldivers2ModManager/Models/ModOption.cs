namespace Helldivers2ModManager.Models;

internal sealed class ModOption
{
	public required string Name { get; init; }

	public required string Description { get; init; }

	public string[]? Include { get; init; }

	public string? Image { get; init; }

	public ModSubOption[]? SubOptions { get; init; }
}
