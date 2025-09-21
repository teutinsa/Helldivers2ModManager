using System.Text.Json;

namespace Helldivers2ModManager.Extensions;

internal static class JsonElementExtensions
{
    public static bool TryGetProperty(this JsonElement node, string propertyName, JsonValueKind valueKind, out JsonElement value)
    {
        if (node.TryGetProperty(propertyName, out var prop) && prop.ValueKind == valueKind)
        {
            value = prop;
            return true;
        }

        value = default;
        return false;
    }
}