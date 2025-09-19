using System.Text.Json;

namespace Helldivers2ModManager.Models;

public sealed class LegacyModManifest : IModManifest
{
    public ManifestVersion Version => ManifestVersion.Legacy;
    
    public required Guid Guid { get; init; }
    
    public required string Name { get; init; }

    public required string Description { get; init; }
    
    public string? IconPath { get; init; }
    
    public IReadOnlyList<string>? Options { get; init; }

    public static IModManifest Deserialize(JsonElement root)
    {
        throw new NotImplementedException();
    }

    public void Serialize(Utf8JsonWriter writer)
    {
        throw new NotImplementedException();
    }
}