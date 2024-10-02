namespace Helldivers2ModManager;

internal readonly ref struct Result<TOk, TErr>
{
	private enum ResultKind
	{
		Err,
		Ok
	}

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

	private readonly bool _isOk;
	private readonly TOk _ok;
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
