using System.Runtime.InteropServices;

namespace Helldivers2ModManager;

internal readonly ref struct Result<TErr>
{
	public TErr Err
	{
		get
		{
			if (_isOk)
				throw new InvalidOperationException("Can not retrieve 'Err' value as the result does not contain an error.");
			return _err;
		}
	}

	public bool IsOk => _isOk;

	public bool IsErr => !_isOk;

	private readonly bool _isOk;
	private readonly TErr _err;

	public Result()
	{
		_isOk = true;
		_err = default!;
	}

	public Result(TErr err)
	{
		_isOk = true;
		_err = err;
	}


	public static implicit operator Result<TErr>(TErr err)
	{
		return new Result<TErr>(err);
	}
}

[StructLayout(LayoutKind.Explicit)]
internal readonly ref struct Result<TOk, TErr>
{
	public TOk Ok
	{
		get
		{
			if (!_isOk)
				throw new InvalidOperationException("Can not retrieve 'Ok' value as the result contains an error.");
			return _ok;
		}
	}

	public TErr Err
	{
		get
		{
			if (_isOk)
				throw new InvalidOperationException("Can not retrieve 'Err' value as the result does not contain an error.");
			return _err;
		}
	}

	public bool IsOk => _isOk;

	public bool IsErr => !_isOk;

	[FieldOffset(0)]
	private readonly bool _isOk;
	[FieldOffset(sizeof(bool))]
	private readonly TOk _ok;
	[FieldOffset(sizeof(bool))]
	private readonly TErr _err;

	public Result(TOk ok)
	{
		_isOk = true;
		_ok = ok;
		_err = default!;
	}

	public Result(TErr err)
	{
		_isOk = false;
		_ok = default!;
		_err = err;
	}

	public static implicit operator Result<TOk, TErr>(TOk ok)
	{
		return new Result<TOk, TErr>(ok);
	}

	public static implicit operator Result<TOk, TErr>(TErr err)
	{
		return new Result<TOk, TErr>(err);
	}
}
