using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Models;

internal interface IJsonSerializable<out T> where T : IJsonSerializable<T>
{
    static abstract T Deserialize(JsonElement root, ILogger? logger = null);

    void Serialize(Utf8JsonWriter writer);
}