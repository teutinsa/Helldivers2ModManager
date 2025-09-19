namespace Helldivers2ModManager.Models;

public sealed class ModOption
{
    public required string Name { get; init; }
    
    public required string Description { get; init; }
    
    public IReadOnlyList<string>? Include { get; init; }
    
    public string? Image { get; init; }
    
    public IReadOnlyList<ModSubOption>? SubOptions { get; init; }
}