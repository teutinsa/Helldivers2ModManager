using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Models;

internal interface IJsonInplaceSerializable
{
	void Deserialize(JsonElement root, ILogger? logger = null);

	void Serialize(Utf8JsonWriter writer);
}