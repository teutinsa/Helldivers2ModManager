using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helldivers2ModManager.Models
{
	internal sealed class ModManifestJsonConverter : JsonConverter<ModManifest>
	{
		public override ModManifest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;

			Guid guid = default;
			string? name = default!;
			string? description = default!;
			string? iconPath = null;
			string[]? options = null;

			if (root.TryGetProperty(nameof(ModManifest.Guid), out var prop))
				_ = Guid.TryParse(prop.GetString(), out guid);
			if (root.TryGetProperty(nameof(ModManifest.Name), out prop))
				name = prop.GetString();
			if (root.TryGetProperty(nameof(ModManifest.Description), out prop))
				description = prop.GetString();
			if (root.TryGetProperty(nameof(ModManifest.IconPath), out prop))
				iconPath = prop.GetString();
			if (root.TryGetProperty(nameof(ModManifest.Options), out prop))
			{
				options = new string[prop.GetArrayLength()];
				int i = 0;
				foreach (var elm in prop.EnumerateArray())
					options[i++] = elm.GetString() ?? throw new JsonException("Expected `string` in 'Options' array!");
			}

			if (guid == default)
				throw new JsonException("Expected `System.Guid`!");
			if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
				throw new JsonException("Expected `string` as value of 'Name' property!");
			if (description is null)
				throw new JsonException("Expected `string` as value of 'Description' property!");

			return new ModManifest
			{
				Guid = guid,
				Name = name,
				Description = description,
				IconPath = iconPath,
				Options = options,
			};
		}

		public override void Write(Utf8JsonWriter writer, ModManifest value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			writer.WriteString(nameof(ModManifest.Guid), value.Guid);
			writer.WriteString(nameof(ModManifest.Name), value.Name);
			writer.WriteString(nameof(ModManifest.Description), value.Description);
			if (value.IconPath is not null)
				writer.WriteString(nameof(ModManifest.IconPath), value.IconPath);
			if(value.Options is not null)
			{
				writer.WriteStartArray(nameof(ModManifest.Options));
				foreach (var option in value.Options)
					writer.WriteStringValue(option);
				writer.WriteEndArray();
			}

			writer.WriteEndObject();
		}
	}
}
