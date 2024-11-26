using System.IO;
using System.Text.Json;

namespace Helldivers2ModManager.Services.Nexus;

internal sealed class NexusMod
{
	public bool Available
	{
		get
		{
			if (!_available.HasValue)
				_available = _document.RootElement.GetProperty("available").GetBoolean();
			return _available.Value;
		}
	}

	public string Name
	{
		get
		{
			_name ??= _document.RootElement.GetProperty("name").GetString() ?? throw new Exception();
			return _name;
		}
	}

	public string Summary
	{
		get
		{
			_summary ??= _document.RootElement.GetProperty("summary").GetString() ?? throw new Exception();
			return _summary;
		}
	}

	public Uri? PrictureUrl => _prictureUrl.Value;

	public int ModId
	{
		get
		{
			if (!_modId.HasValue)
				_modId = _document.RootElement.GetProperty("mod_id").GetInt32();
			return _modId.Value;
		}
	}

	public string Version
	{
		get
		{
			_version ??= _document.RootElement.GetProperty("version").GetString() ?? throw new Exception();
			return _version;
		}
	}

	public string Author
	{
		get
		{
			_author ??= _document.RootElement.GetProperty("author").GetString() ?? throw new Exception();
			return _author;
		}
	}
    public string Uploader
    {
        get
        {
            _uploader ??= _document.RootElement.GetProperty("uploaded_by").GetString() ?? throw new Exception();
            return _uploader;
        }
    }

    private readonly JsonDocument _document;
	private bool? _available;
	private string? _name;
	private string? _summary;
	private Lazy<Uri?> _prictureUrl;
	private int? _modId;
	private string? _version;
	private string? _author;
	private string? _uploader;


    private NexusMod(JsonDocument document)
	{
		_document = document;
		_prictureUrl = new(() =>
		{
			if (_document.RootElement.TryGetProperty("picture_url", out var elm))
				return new Uri(elm.GetString() ?? throw new Exception());
			return null;
		});
	}

	public static async Task<NexusMod> CreateAsync(Stream utf8Json, CancellationToken cancellationToken = default)
	{
		return new NexusMod(await JsonDocument.ParseAsync(utf8Json, default, cancellationToken));
	}
}
