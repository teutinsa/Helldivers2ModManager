using System.Text.Json;
using Helldivers2ModManager.Extensions;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Models;

internal sealed class ModSubOption : IJsonSerializable<ModSubOption>
{
    public required string Name { get; init; }
    
    public required string Description { get; init; }
    
    public required IReadOnlyList<string> Include { get; init; }
    
    public string? Image { get; init; }
    
    public static ModSubOption Deserialize(JsonElement root, ILogger? logger = null)
    {
        var name = root.GetProperty(nameof(Name)).GetString()!;
        var description = root.GetProperty(nameof(Description)).GetString()!;
        var prop = root.GetProperty(nameof(Include));
        var include = new List<string>(prop.GetArrayLength());
        foreach (var elm in prop.EnumerateArray())
            if (elm.ValueKind == JsonValueKind.String)
                include.Add(elm.GetString()!);
            else
                logger?.LogWarning("Unexpected none `string` value found in mod sub-options includes");
        string? image = null;
        if (root.TryGetProperty(nameof(Image), JsonValueKind.String, out prop))
            image = prop.GetString()!;

        return new ModSubOption
        {
            Name = name,
            Description = description,
            Include = include,
            Image = image,
        };
    }

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(Name), Name);
        writer.WriteString(nameof(Description), Description);
        writer.WriteStartArray(nameof(Include));
        foreach (var inc in Include)
            writer.WriteStringValue(inc);
        writer.WriteEndArray();
        writer.WriteString(nameof(Image), Image);
        writer.WriteEndObject();
    }
}