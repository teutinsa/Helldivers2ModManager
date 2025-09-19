using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Helldivers2ModManager.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Transient)]
internal sealed class SettingsService : IDisposable
{
    public const float OpacityMax = 1.0f;
    
    public const float OpacityMin = 0.4f;
    
    [MemberNotNull(nameof(_stream), nameof(_gameDirectory), nameof(_storageDirectory), nameof(_tempDirectory), nameof(_skipList))]
    public bool Initialized { get; private set; }
    
    public bool IsReadonly { get; private set; } = true;

    public string GameDirectory
    {
        get
        {
            GuardInitialized();
            return _gameDirectory;
        }

        set
        {
            GuardInitialized();
            GuardReadonly();
            _gameDirectory = value;
        }
    }

    public string StorageDirectory
    {
        get
        {
            GuardInitialized();
            return _storageDirectory;
        }

        set
        {
            GuardInitialized();
            GuardReadonly();
            _storageDirectory = value;
        }
    }

    public string TempDirectory
    {
        get
        {
            GuardInitialized();
            return _tempDirectory;
        }

        set
        {
            GuardInitialized();
            GuardReadonly();
            _tempDirectory = value;
        }
    }

    public LogLevel LogLevel
    {
        get
        {
            GuardInitialized();
            return _logLevel;
        }

        set
        {
            GuardInitialized();
            GuardReadonly();
            _logLevel = value;
        }
    }

    public float Opacity
    {
        get
        {
            GuardInitialized();
            return _opacity;
        }

        set
        {
            GuardInitialized();
            GuardReadonly();
            _opacity = Math.Clamp(value, OpacityMin, OpacityMax);
        }
    }

    public ObservableCollection<string> SkipList
    {
        get
        {
            GuardInitialized();
            return _skipList;
        }
    }

    public bool CaseSensitiveSearch
    {
        get
        {
            GuardInitialized();
            return _caseSensitiveSearch;
        }

        set
        {
            GuardInitialized();
            GuardReadonly();
            _caseSensitiveSearch = value;
        }
    }

    public event EventHandler? Changed;
    
    private static readonly FileInfo s_file = new("settings.json");
    private static readonly JsonDocumentOptions s_options = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly ILogger<SettingsService> _logger;
    private FileStream? _stream;
    private FileSystemWatcher? _watcher;
    private string _gameDirectory = null!;
    private string _storageDirectory = null!;
    private string _tempDirectory = null!;
    private LogLevel _logLevel;
    private float _opacity;
    private ObservableCollection<string> _skipList = null!;
    private bool _caseSensitiveSearch;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _watcher?.Dispose();
    }
    
    [MemberNotNullWhen(true, nameof(_gameDirectory), nameof(_storageDirectory), nameof(_tempDirectory), nameof(_skipList))]
    public async Task<bool> InitAsync(bool @readonly = false)
    {
        if (Initialized)
            return true;
        
        if (!s_file.Exists)
            return false;

        _stream = s_file.Open(FileMode.Open, IsReadonly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.ReadWrite);
        _watcher = new FileSystemWatcher();
        _watcher.Path = s_file.DirectoryName ?? string.Empty;
        _watcher.Filter = s_file.Name;
        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        _watcher.IncludeSubdirectories = false;
        _watcher.EnableRaisingEvents = true;
        _watcher.Changed += WatcherOnChanged;
        
        Reset();

        ReadAsync();
        
        IsReadonly = @readonly;
        Initialized = true;
        return true;
    }

    [MemberNotNull(nameof(_gameDirectory), nameof(_storageDirectory), nameof(_tempDirectory), nameof(_skipList))]
    public void InitDefault(bool @readonly = false)
    {
        if (Initialized)
            return;
        
        Reset();
        
        IsReadonly = @readonly;
        Initialized = true;
    }

    [MemberNotNull(nameof(_gameDirectory), nameof(_storageDirectory), nameof(_tempDirectory), nameof(_skipList))]
    public void Reset()
    {
        GuardReadonly();
        
        _gameDirectory = string.Empty;
        _storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Helldivers2ModManager");
        _tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "Helldivers2ModManager");
        _logLevel = LogLevel.Warning;
        _opacity = 0.8f;
        _skipList = [];
        _caseSensitiveSearch = false;
    }

    public async Task SaveAsync()
    {
        GuardInitialized();
        GuardReadonly();

        var writer = new Utf8JsonWriter(_stream);
        
        writer.WriteStartObject();
            writer.WriteString(nameof(GameDirectory), _gameDirectory);
            writer.WriteString(nameof(StorageDirectory), _storageDirectory);
            writer.WriteString(nameof(TempDirectory), _tempDirectory);
            writer.WriteString(nameof(LogLevel), _logLevel.ToString());
            writer.WriteNumber(nameof(Opacity), _opacity);
                writer.WriteStartArray(nameof(SkipList));
                    foreach (var elm in _skipList)
                        writer.WriteStringValue(elm);
                writer.WriteEndArray();
            writer.WriteBoolean(nameof(CaseSensitiveSearch), _caseSensitiveSearch);
        writer.WriteEndObject();
        
        await writer.DisposeAsync();
    }

    public bool Validate()
    {
        GuardInitialized();

        if (!Directory.Exists(_gameDirectory))
            try
            {
                Directory.CreateDirectory(_gameDirectory);
            }
            catch
            {
                return false;
            }
        
        if (!Directory.Exists(_storageDirectory))
            try
            {
                Directory.CreateDirectory(_storageDirectory);
            }
            catch
            {
                return false;
            }

        if (!Directory.Exists(_tempDirectory))
            try
            {
                Directory.CreateDirectory(_tempDirectory);
            }
            catch
            {
                return false;
            }

        if (!Enum.GetValues<LogLevel>().Contains(_logLevel))
        {
            if (IsReadonly)
                return false;
            _logLevel = LogLevel.Trace;
        }

        if (_opacity is > OpacityMax or < OpacityMin)
        {
            if (IsReadonly)
                return false;
            _opacity = Math.Clamp(_opacity, OpacityMin, OpacityMax);
        }

        var elms = _skipList.Where(static elm => elm.Length != 16).ToArray();
        if (elms.Length != 0)
        {
            if (IsReadonly)
                return false;
            foreach (var elm in elms)
                _skipList.Remove(elm);
        }
        
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GuardReadonly()
    {
        if (IsReadonly)
            throw new InvalidOperationException("Object is readonly!");
    }
    
    [MemberNotNull(nameof(_stream), nameof(_gameDirectory), nameof(_storageDirectory), nameof(_tempDirectory), nameof(_skipList))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GuardInitialized()
    {
        if (!Initialized)
            throw new InvalidOperationException("Object not initialized!");
    }

    private async Task ReadAsync()
    {
        if (_stream is null)
            return;
        
        _stream.Seek(0, SeekOrigin.Begin);
        
        using var document = await JsonDocument.ParseAsync(_stream, s_options);
        var root = document.RootElement;
        if (root.TryGetProperty(nameof(GameDirectory), JsonValueKind.String, out var prop))
            _gameDirectory = prop.GetString()!;
        if (root.TryGetProperty(nameof(StorageDirectory), JsonValueKind.String, out prop))
            _storageDirectory = prop.GetString()!;
        if (root.TryGetProperty(nameof(TempDirectory), JsonValueKind.String, out prop))
            _tempDirectory = prop.GetString()!;
        if (root.TryGetProperty(nameof(LogLevel), JsonValueKind.String, out prop))
            if (Enum.TryParse<LogLevel>(prop.GetString()!, out var value))
                _logLevel = value;
        if (root.TryGetProperty(nameof(Opacity), JsonValueKind.Number, out prop))
            if (prop.TryGetSingle(out var value))
                _opacity = value;
        if (root.TryGetProperty(nameof(SkipList), JsonValueKind.Array, out var arr))
        {
            var list = new List<string>(arr.GetArrayLength());
            
            foreach (var elm in arr.EnumerateArray())
                if (elm.ValueKind == JsonValueKind.String)
                {
                    var value = elm.GetString();
                    if (value is not null)
                        list.Add(value);
                }

            _skipList = new ObservableCollection<string>(list);
        }
        if (root.TryGetProperty(nameof(CaseSensitiveSearch), out prop) && prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
            _caseSensitiveSearch = prop.GetBoolean();
    }

    private async void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            await ReadAsync();
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while handling {nameof(WatcherOnChanged)} event");
        }
    }
}