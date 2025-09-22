namespace Helldivers2ModManager.Extensions;

internal static class TypeExtension
{
    public static bool IsNumber(this Type type)
    {
        var code = Type.GetTypeCode(type);
		return code.IsNumber();
    }

    public static bool IsNumber(this TypeCode code)
    {
		return code switch
		{
			TypeCode.SByte or
			TypeCode.Byte or
			TypeCode.Int16 or
			TypeCode.UInt16 or
			TypeCode.Int32 or
			TypeCode.UInt32 or
			TypeCode.Int64 or
			TypeCode.UInt64 or
			TypeCode.Single or
			TypeCode.Double or
			TypeCode.Decimal => true,
			_ => false,
		};
	}
}
