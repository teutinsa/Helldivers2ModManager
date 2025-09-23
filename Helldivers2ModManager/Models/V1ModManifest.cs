using System;
using System.Text.Json;
using Helldivers2ModManager.Extensions;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Models;

internal sealed class V1ModManifest : IModManifest
{
    public ManifestVersion Version => ManifestVersion.V1;
    
    public required Guid Guid { get; init; }
    
    public required string Name { get; init; }

    public required string Description { get; init; }
    
    public string? IconPath { get; init; }
    
    public IReadOnlyList<ModOption>? Options { get; init; }
    
    public static IModManifest Deserialize(JsonElement root, ILogger? logger = null)
    {
        var guid = Guid.Parse(root.GetProperty<string>(nameof(Guid)));
        var name = root.GetProperty<string>(nameof(Name));
        var description = root.GetProperty<string>(nameof(Description));
        string? iconPath = null;
        if (root.TryGetProperty(nameof(IconPath), JsonValueKind.String, out var prop))
            iconPath = prop.GetString()!;
        List<ModOption>? options = null;
        if (root.TryGetProperty(nameof(Options), JsonValueKind.Array, out prop))
        {
            options = new(prop.GetArrayLength());
            foreach (var elm in prop.EnumerateArray())
                if (elm.ValueKind == JsonValueKind.Object)
                    options.Add(ModOption.Deserialize(elm));
                else
                    logger?.LogWarning("Unexpected none `object` value found in v1 manifest options");
        }

        return new V1ModManifest
        {
            Guid = guid,
            Name = name,
            Description = description,
            IconPath = iconPath,
            Options = options,
        };
    }

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(Guid), Guid.ToString());
        writer.WriteString(nameof(Name), Name);
        writer.WriteString(nameof(Description), Description);
        if (IconPath is not null)
            writer.WriteString(nameof(IconPath), IconPath);
        if (Options is not null)
        {
            writer.WriteStartArray(nameof(Options));
            foreach (var opt in Options)
                opt.Serialize(writer);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}