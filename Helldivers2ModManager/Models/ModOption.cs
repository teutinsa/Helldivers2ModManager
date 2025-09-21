using System.Text.Json;
using Helldivers2ModManager.Extensions;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Models;

internal sealed class ModOption : IJsonSerializable<ModOption>
{
    public required string Name { get; init; }
    
    public required string Description { get; init; }
    
    public IReadOnlyList<string>? Include { get; init; }
    
    public string? Image { get; init; }
    
    public IReadOnlyList<ModSubOption>? SubOptions { get; init; }
    
    public static ModOption Deserialize(JsonElement root, ILogger? logger = null)
    {
        var name = root.GetProperty(nameof(Name)).GetString()!;
        var description = root.GetProperty(nameof(Description)).GetString()!;
        List<string>? include = null;
        if (root.TryGetProperty(nameof(Include), JsonValueKind.Array, out var prop))
        {
            include = new(prop.GetArrayLength());
            foreach (var elm in prop.EnumerateArray())
                if (elm.ValueKind == JsonValueKind.String)
                    include.Add(elm.GetString()!);
                else
                    logger?.LogWarning("Unexpected none `string` value found in mod options includes");
        }
        string? image = null;
        if (root.TryGetProperty(nameof(Image), JsonValueKind.String, out prop))
            image = prop.GetString()!;
        List<ModSubOption>? subOptions = null;
        if (root.TryGetProperty(nameof(SubOptions), JsonValueKind.Array, out prop))
        {
            subOptions = new(prop.GetArrayLength());
            foreach (var elm in prop.EnumerateArray())
                if (elm.ValueKind == JsonValueKind.Object)
                    subOptions.Add(ModSubOption.Deserialize(elm));
                else
                    logger?.LogWarning("Unexpected none `object` value found in mod options sub-options");
        }

        return new ModOption
        {
            Name = name,
            Description = description,
            Include = include,
            Image = image,
            SubOptions = subOptions,
        };
    }

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(Name), Name);
        writer.WriteString(nameof(Description), Description);
        if (Include is not null)
        {
            writer.WriteStartArray(nameof(Include));
            foreach (var inc in Include)
                writer.WriteStringValue(inc);
            writer.WriteEndArray();
        }
        if (Image is not null)
            writer.WriteString(nameof(Image), Image);
        if (SubOptions is not null)
        {
            writer.WriteStartArray(nameof(SubOptions));
            foreach (var sub in SubOptions)
                sub.Serialize(writer);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}