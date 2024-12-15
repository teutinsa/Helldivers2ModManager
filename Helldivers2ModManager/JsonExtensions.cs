using System.Runtime.Serialization;
using System.Text.Json;

namespace Helldivers2ModManager;

internal static class JsonExtensions
{
	public static string ExpectStringProp(this JsonElement elm, string name)
	{
		return elm.GetProperty(name).GetString() ?? throw new SerializationException($"Expected property \"{name}\" of type `string`!");
	}

	public static string? OptionalStringProp(this JsonElement elm, string name)
	{
		if (elm.TryGetProperty(name, out var prop))
			return prop.GetString();
		return null;
	}

	public static string[] ExpectStringArrayProp(this JsonElement elm, string name)
	{
		var arr = elm.GetProperty(name);
		var len = arr.GetArrayLength();
		var values = new string[len];
		var i = 0;
		foreach (var item in arr.EnumerateArray())
		{
			values[i] = item.GetString() ?? throw new SerializationException($"Expected element of array \"{name}\" to be of type `string`!");
			i++;
		}
		return values;
	}

	public static string[]? OptionalStringArrayProp(this JsonElement elm, string name)
	{
		if (elm.TryGetProperty(name, out var arr))
		{
			var len = arr.GetArrayLength();
			var values = new string[len];
			var i = 0;
			foreach (var item in arr.EnumerateArray())
			{
				values[i] = item.GetString() ?? throw new SerializationException($"Expected element of array \"{name}\" to be of type `string`!");
				i++;
			}
			return values;
		}
		return null;
	}
}
