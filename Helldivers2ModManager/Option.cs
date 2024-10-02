namespace Helldivers2ModManager;

internal readonly struct Option<T>
{
	public T Value
	{
		get
		{
			if (!_isSome)
				throw new InvalidOperationException();
			return _value;
		}
	}

	public bool IsSome => _isSome;

	public bool IsNone => !_isSome;

	private readonly bool _isSome;
	private readonly T _value;

	public Option(T value)
	{
		_isSome = true;
		_value = value;
	}

	public static implicit operator Option<T>(T value)
	{
		return new Option<T>(value);
	}
}
