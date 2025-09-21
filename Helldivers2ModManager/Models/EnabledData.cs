using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Helldivers2ModManager.Models;

internal readonly struct EnabledData : IJsonSerializable<EnabledData>
{
	public required Guid Guid { get; init; }

	public required bool Enabled { get; init; }

	public required bool[] Toggled { get; init; }

	public required int[] Selected { get; init; }

	public static EnabledData Deserialize(JsonElement root, ILogger? logger = null)
	{
		var guid = Guid.Parse(root.GetProperty(nameof(Guid)).GetString()!);

		var enabled = root.GetProperty(nameof(Enabled)).GetBoolean();

		var prop = root.GetProperty(nameof(Toggled));
		if (prop.ValueKind != JsonValueKind.Array)
			throw new SerializationException($"Expected property `{nameof(Toggled)}` to be of type `array`!");
		var toggled = new bool[prop.GetArrayLength()];
		var arr = prop.EnumerateArray().ToArray();
		for (int i = 0; i < arr.Length; i++)
			toggled[i] = arr[i].GetBoolean();

		prop = root.GetProperty(nameof(Selected));
		if (prop.ValueKind != JsonValueKind.Array)
			throw new SerializationException($"Expected property `{nameof(Selected)}` to be of type `array`!");
		var selected = new int[prop.GetArrayLength()];
		arr = prop.EnumerateArray().ToArray();
		for (int i = 0; i < arr.Length; i++)
			selected[i] = arr[i].GetInt32();

		return new EnabledData
		{
			Guid = guid,
			Enabled = enabled,
			Toggled = toggled,
			Selected = selected,
		};
	}

	public void Serialize(Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(Guid), Guid.ToString());
		writer.WriteBoolean(nameof(Enabled), Enabled);
		writer.WriteStartArray(nameof(Toggled));
		foreach (var elm in Toggled)
			writer.WriteBooleanValue(elm);
		writer.WriteEndArray();
		writer.WriteStartArray(nameof(Selected));
		foreach (var elm in Selected)
			writer.WriteNumberValue(elm);
		writer.WriteEndArray();
		writer.WriteEndObject();
	}

	public override string ToString()
	{
		return $"{{ {nameof(Guid)} = \"{{{Guid}}}\", {nameof(Enabled)} = {Enabled}, {nameof(Toggled)} = {string.Join(", ", Toggled)}, {nameof(Selected)} = {string.Join(", ", Selected)} }}";
	}
}
