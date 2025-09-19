using System.Text.Json;

namespace Helldivers2ModManager.Models;

public interface IModManifest
{
    ManifestVersion Version { get; }
    
    Guid Guid { get; }
    
    public string Name { get; }
    
    public string Description { get; }
    
    public string? IconPath { get; }
    
    static abstract IModManifest Deserialize(JsonElement root);
    
    void Serialize(Utf8JsonWriter writer);
}