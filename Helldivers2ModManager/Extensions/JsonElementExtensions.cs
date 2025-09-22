using System.Runtime.Serialization;
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

	public static JsonElement GetProperty(this JsonElement node, string propertyName, JsonValueKind valueKind)
	{
		if (!node.TryGetProperty(propertyName, out var prop))
			throw new SerializationException($"Could not find property of name \"{propertyName}\"!");
		if (prop.ValueKind != valueKind)
			throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{valueKind.ToString().ToLower()}´!");
		return prop;
	}

	public static T GetProperty<T>(this JsonElement node, string propertyName)
	{
		if (!node.TryGetProperty(propertyName, out var prop))
			throw new SerializationException($"Could not find property of name \"{propertyName}\"!");

		var type = typeof(T);
		var code = Type.GetTypeCode(type);
		switch (code)
		{
			case TypeCode.Boolean:
			{
				if (prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
					return (T)(object)prop.GetBoolean();
				break;
			}

			case TypeCode.Byte:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetByte(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.SByte:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetSByte(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.Int16:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetInt16(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.UInt16:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetUInt16(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.Int32:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetInt32(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.UInt32:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetUInt32(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.Int64:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetInt64(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.UInt64:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetUInt64(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.Single:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetSingle(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.Double:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetDouble(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.Decimal:
			{
				if (prop.ValueKind != JsonValueKind.Number)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.Number.ToString().ToLower()}´!");
				if (prop.TryGetDecimal(out var value))
					return (T)(object)value;
				break;
			}

			case TypeCode.String:
			{
				if (prop.ValueKind != JsonValueKind.String)
					throw new SerializationException($"Property \"{propertyName}\" was not of expected type ´{JsonValueKind.String.ToString().ToLower()}´!");
				if (prop.GetString() is { } value)
					return (T)(object)value;
				break;
			}

			default:
				throw new ArgumentException("Unsupported generic type!", nameof(T));
		}

		throw new SerializationException($"Could not convert value of property \"{propertyName}\" to `{type.Name}`!");
	}
}