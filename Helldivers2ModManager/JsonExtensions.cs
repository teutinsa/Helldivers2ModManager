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

	public static JsonElement[] ExpectObjectArrayProp(this JsonElement elm, string name)
	{
		var arr = elm.GetProperty(name);
		var len = arr.GetArrayLength();
		var objects = new JsonElement[len];
		var i = 0;
		foreach (var item in arr.EnumerateArray())
		{
			if (item.ValueKind != JsonValueKind.Object)
				throw new SerializationException($"Expected element in array \"{name}\" to be of type `object`!");
			objects[i] = item;
			i++;
		}
		return objects;
	}

	public static JsonElement[]? OptionalObjectArrayProp(this JsonElement elm, string name)
	{
		if (elm.TryGetProperty(name, out var arr))
		{
			var len = arr.GetArrayLength();
			var objects = new JsonElement[len];
			var i = 0;
			foreach (var item in arr.EnumerateArray())
			{
				if (item.ValueKind != JsonValueKind.Object)
					throw new SerializationException($"Expected element in array \"{name}\" to be of type `object`!");
				objects[i] = item;
				i++;
			}
			return objects;
		}
		return null;
	}

	public static JsonElement ExpectObjectProp(this JsonElement elm, string name)
	{
		var obj = elm.GetProperty(name);
		if (obj.ValueKind != JsonValueKind.Object)
			throw new SerializationException($"Expected property \"{name}\" of type `object`!");
		return obj;
	}

	public static JsonElement? OptionalObjectProp(this JsonElement elm, string name)
	{
		if (elm.TryGetProperty(name, out var obj))
		{
			if (obj.ValueKind != JsonValueKind.Object)
				throw new SerializationException($"Expected property \"{name}\" of type `object`!");
			return obj;
		}
		return null;
	}

	public static int ExpectInt32Prop(this JsonElement elm, string name)
	{
		return elm.GetProperty(name).GetInt32();
	}

	public static uint ExpectUInt32Prop(this JsonElement elm, string name)
	{
		return elm.GetProperty(name).GetUInt32();
	}

	public static bool ExpectBoolean(this JsonElement elm, string name)
	{
		return elm.GetProperty(name).GetBoolean();
	}

	public static int[] ExpectIntArrayProp(this JsonElement elm, string name)
	{
		var arr = elm.GetProperty(name);
		var len = arr.GetArrayLength();
		var values = new int[len];
		var i = 0;
		foreach (var item in arr.EnumerateArray())
		{
			values[i] = item.GetInt32();
			i++;
		}
		return values;
	}

	public static bool[] ExpectBooleanArrayProp(this JsonElement elm, string name)
	{
		var arr = elm.GetProperty(name);
		var len = arr.GetArrayLength();
		var values = new bool[len];
		var i = 0;
		foreach (var item in arr.EnumerateArray())
		{
			values[i] = item.GetBoolean();
			i++;
		}
		return values;
	}
}
