using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Models;

internal interface IModManifest
{
    ManifestVersion Version { get; }
    
    Guid Guid { get; }
    
    public string Name { get; }
    
    public string Description { get; }
    
    public string? IconPath { get; }
    
    static abstract IModManifest Deserialize(JsonElement root, ILogger? logger = null);
    
    void Serialize(Utf8JsonWriter writer);
}