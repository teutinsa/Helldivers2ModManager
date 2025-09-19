using System.Text.Json;

namespace Helldivers2ModManager.Models;

public sealed class V1ModManifest : IModManifest
{
    public ManifestVersion Version => ManifestVersion.V1;
    
    public required Guid Guid { get; init; }
    
    public required string Name { get; init; }

    public required string Description { get; init; }
    
    public string? IconPath { get; init; }
    
    public IReadOnlyList<ModOption>? Options { get; init; }
    
    public static IModManifest Deserialize(JsonElement root)
    {
        throw new NotImplementedException();
    }

    public void Serialize(Utf8JsonWriter writer)
    {
        throw new NotImplementedException();
    }
}