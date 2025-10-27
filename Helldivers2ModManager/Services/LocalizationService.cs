using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Singleton)]
internal sealed class LocalizationService : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection<string> AvailableLanguages { get; } = new();
	
	public string CurrentLanguage
	{
		get => _currentLanguage;
		set
		{
			if (_currentLanguage != value)
			{
				_currentLanguage = value;
				LoadLanguageAsync(value).ConfigureAwait(false);
				OnPropertyChanged();
			}
		}
	}

	public string this[string key]
	{
		get
		{
			if (_translations.TryGetValue(key, out var value))
				return value;
			
			_logger.LogWarning("Translation key not found: {}", key);
			return $"[{key}]";
		}
	}

	private static readonly DirectoryInfo s_languageDir = new("Languages");
	private readonly ILogger<LocalizationService> _logger;
	private readonly Dictionary<string, string> _translations = new();
	private string _currentLanguage = "en";

	public LocalizationService(ILogger<LocalizationService> logger)
	{
		_logger = logger;
	}

	public async Task InitializeAsync(string language = "en")
	{
		_logger.LogInformation("Initializing localization service");
		
		if (!s_languageDir.Exists)
		{
			_logger.LogWarning("Language directory not found, creating default");
			s_languageDir.Create();
			await CreateDefaultLanguageFilesAsync();
		}

		LoadAvailableLanguages();
		
		if (!AvailableLanguages.Contains(language))
			language = "en";
		
		await LoadLanguageAsync(language);
		_currentLanguage = language;
		
		_logger.LogInformation("Localization service initialized with language: {}", language);
	}

	private void LoadAvailableLanguages()
	{
		AvailableLanguages.Clear();
		
		var files = s_languageDir.GetFiles("*.json");
		foreach (var file in files)
		{
			var langCode = Path.GetFileNameWithoutExtension(file.Name);
			AvailableLanguages.Add(langCode);
		}

		if (AvailableLanguages.Count == 0)
		{
			AvailableLanguages.Add("en");
		}
	}

	private async Task LoadLanguageAsync(string language)
	{
		_logger.LogInformation("Loading language: {}", language);
		
		var filePath = Path.Combine(s_languageDir.FullName, $"{language}.json");
		if (!File.Exists(filePath))
		{
			_logger.LogWarning("Language file not found: {}", filePath);
			return;
		}

		try
		{
			var json = await File.ReadAllTextAsync(filePath);
			var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
			
			if (translations != null)
			{
				_translations.Clear();
				foreach (var kvp in translations)
				{
					_translations[kvp.Key] = kvp.Value;
				}
				
				// Notify all properties changed to update UI
				OnPropertyChanged(string.Empty);
				_logger.LogInformation("Loaded {} translations", _translations.Count);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load language file: {}", filePath);
		}
	}

	private async Task CreateDefaultLanguageFilesAsync()
	{
		// Create English language file
		var enTranslations = new Dictionary<string, string>
		{
			// Main Window
			["Window.Title"] = "Mod Manager",
			["Window.Help"] = "?",
			
			// Dashboard Page
			["Dashboard.Title"] = "Dashboard",
			["Dashboard.Add"] = "Add",
			["Dashboard.ReportBug"] = "Report Bug",
			["Dashboard.Settings"] = "Settings",
			["Dashboard.Purge"] = "Purge",
			["Dashboard.Deploy"] = "Deploy",
			["Dashboard.LaunchHD2"] = "Launch HD2",
			["Dashboard.Search"] = "Search:",
			["Dashboard.Edit"] = "Edit",
			["Dashboard.PurgeTooltip"] = "Removes all patch files from the games data directory.",
			["Dashboard.DeployTooltip"] = "Installs all selected mods patch files into the games data directory.",
			["Dashboard.LaunchTooltip"] = "Runs the game through steam.",
			
			// Settings Page
			["Settings.Title"] = "Settings",
			["Settings.GameDirectory"] = "Game Directory",
			["Settings.GameDirectoryDesc"] = "This is the games directory where you want the mods to be installed.",
			["Settings.GameDirectoryHint"] = "(Clicking \"...\" will prompt you to select the games directory)",
			["Settings.AutoDetect"] = "Auto detect",
			["Settings.StorageDirectory"] = "Storage Directory",
			["Settings.StorageDirectoryDesc"] = "This is where files about all the managed mods are stored.",
			["Settings.StorageDirectoryWarning"] = "Purge before changing this as a record of the installed files is stored in this!",
			["Settings.TempDirectory"] = "Temporary Directory",
			["Settings.TempDirectoryDesc"] = "This is the directory where all temporary files will be stored. Examples are:",
			["Settings.TempDirectoryItem1"] = "- Download files",
			["Settings.TempDirectoryItem2"] = "- Staging files",
			["Settings.TempDirectoryItem3"] = "- Decompressed files",
			["Settings.Opacity"] = "Opacity",
			["Settings.OpacityDesc"] = "Change the opacity of the window background.",
			["Settings.LogLevel"] = "Log Level",
			["Settings.LogLevelDesc"] = "This sets the level of messages which should be logged to the log file. The option set and anything below it will be captured an logged.",
			["Settings.LogLevelDefault"] = "By default only warnings and anything more severe will be logged.",
			["Settings.Search"] = "Search",
			["Settings.CaseSensitive"] = "Case Sensitive",
			["Settings.Utilities"] = "Utilities",
			["Settings.Reset"] = "Reset",
			["Settings.ResetDesc"] = "This will this will reset all the setting to their default values!",
			["Settings.DevOptions"] = "Dev Options",
			["Settings.SkipList"] = "Skip List",
			["Settings.SkipListDesc"] = "This skips the 0th index of all specified files during deployment.",
			["Settings.OK"] = "OK",
			["Settings.Language"] = "Language",
			["Settings.LanguageDesc"] = "Select your preferred language for the interface.",
			
			// Messages
			["Message.LoadingSettings"] = "Loading settings",
			["Message.PleaseWait"] = "Please wait democratically.",
			["Message.LoadingSettingsFailed"] = "Loading settings failed!",
			["Message.ResetSettingsConfirm"] = "Do you want to reset your settings?",
			["Message.SavingSettings"] = "Saving Settings",
			["Message.GameDirEmpty"] = "Game directory can not be left empty!",
			["Message.StorageDirEmpty"] = "Storage directory can not be left empty!",
			["Message.TempDirEmpty"] = "Temporary directory can not be left empty!",
			["Message.InvalidSettings"] = "Invalid settings!",
			["Message.SaveSettingsFailed"] = "Failed to save settings!",
			["Message.ResetTitle"] = "Reset?",
			["Message.ResetConfirm"] = "Do you really want to reset your settings?",
			["Message.GameDirNotExist"] = "The selected Helldivers 2 folder does not exist!",
			["Message.GameDirInvalid"] = "The selected Helldivers 2 folder does not reside in a valid directory!",
			["Message.GameDirNoData"] = "The selected Helldivers 2 root path does not contain a directory named \"data\"!",
			["Message.GameDirNoTools"] = "The selected Helldivers 2 root path does not contain a directory named \"tools\"!",
			["Message.GameDirNoBin"] = "The selected Helldivers 2 root path does not contain a directory named \"bin\"!",
			["Message.GameDirNoExe"] = "The selected Helldivers 2 path does not contain a file named \"helldivers2.exe\" in a folder called \"bin\"!",
			["Message.FileNamePrompt"] = "File name?",
			["Message.FileNameDesc"] = "Please enter the 16 character name of an archive file you want to skip patch 0 for.",
			["Message.FileNameLengthError"] = "Archive file names can only be 16 characters long.",
			["Message.LookingForGame"] = "Looking for game",
			["Message.GameNotFound"] = "Your Helldivers 2 game could not be found automatically. Please set it manually.",
			
			// Dialog Titles
			["Dialog.SelectGameFolder"] = "Please select you Helldivers 2 folder...",
			["Dialog.SelectStorageFolder"] = "Please select a folder where you want this manager to store its mods...",
			["Dialog.SelectTempFolder"] = "Please select a folder which you want this manager to use for temporary files...",
		};

		var enPath = Path.Combine(s_languageDir.FullName, "en.json");
		await File.WriteAllTextAsync(enPath, JsonSerializer.Serialize(enTranslations, new JsonSerializerOptions { WriteIndented = true }));
		
		// Create Chinese (Simplified) language file
		var zhCnTranslations = new Dictionary<string, string>
		{
			// Main Window
			["Window.Title"] = "模组管理器",
			["Window.Help"] = "?",
			
			// Dashboard Page
			["Dashboard.Title"] = "控制面板",
			["Dashboard.Add"] = "添加",
			["Dashboard.ReportBug"] = "报告错误",
			["Dashboard.Settings"] = "设置",
			["Dashboard.Purge"] = "清理",
			["Dashboard.Deploy"] = "部署",
			["Dashboard.LaunchHD2"] = "启动游戏",
			["Dashboard.Search"] = "搜索：",
			["Dashboard.Edit"] = "编辑",
			["Dashboard.PurgeTooltip"] = "从游戏数据目录中删除所有补丁文件。",
			["Dashboard.DeployTooltip"] = "将所有选定的模组补丁文件安装到游戏数据目录中。",
			["Dashboard.LaunchTooltip"] = "通过Steam运行游戏。",
			
			// Settings Page
			["Settings.Title"] = "设置",
			["Settings.GameDirectory"] = "游戏目录",
			["Settings.GameDirectoryDesc"] = "这是您希望安装模组的游戏目录。",
			["Settings.GameDirectoryHint"] = "（点击三个点按钮将提示您选择游戏目录）",
			["Settings.AutoDetect"] = "自动检测",
			["Settings.StorageDirectory"] = "存储目录",
			["Settings.StorageDirectoryDesc"] = "这是存储所有管理的模组文件的位置。",
			["Settings.StorageDirectoryWarning"] = "更改此设置前请先清理，因为已安装文件的记录存储在此处！",
			["Settings.TempDirectory"] = "临时目录",
			["Settings.TempDirectoryDesc"] = "这是存储所有临时文件的目录。例如：",
			["Settings.TempDirectoryItem1"] = "- 下载文件",
			["Settings.TempDirectoryItem2"] = "- 暂存文件",
			["Settings.TempDirectoryItem3"] = "- 解压文件",
			["Settings.Opacity"] = "不透明度",
			["Settings.OpacityDesc"] = "更改窗口背景的不透明度。",
			["Settings.LogLevel"] = "日志级别",
			["Settings.LogLevelDesc"] = "设置应记录到日志文件的消息级别。将捕获设置的选项及其以下的所有内容。",
			["Settings.LogLevelDefault"] = "默认情况下，仅记录警告及更严重的消息。",
			["Settings.Search"] = "搜索",
			["Settings.CaseSensitive"] = "区分大小写",
			["Settings.Utilities"] = "实用工具",
			["Settings.Reset"] = "重置",
			["Settings.ResetDesc"] = "这将把所有设置重置为默认值！",
			["Settings.DevOptions"] = "开发者选项",
			["Settings.SkipList"] = "跳过列表",
			["Settings.SkipListDesc"] = "在部署期间跳过所有指定文件的第0个索引。",
			["Settings.OK"] = "确定",
			["Settings.Language"] = "语言",
			["Settings.LanguageDesc"] = "选择您喜欢的界面语言。",
			
			// Messages
			["Message.LoadingSettings"] = "正在加载设置",
			["Message.PleaseWait"] = "请民主地等待。",
			["Message.LoadingSettingsFailed"] = "加载设置失败！",
			["Message.ResetSettingsConfirm"] = "您想重置您的设置吗？",
			["Message.SavingSettings"] = "正在保存设置",
			["Message.GameDirEmpty"] = "游戏目录不能为空！",
			["Message.StorageDirEmpty"] = "存储目录不能为空！",
			["Message.TempDirEmpty"] = "临时目录不能为空！",
			["Message.InvalidSettings"] = "设置无效！",
			["Message.SaveSettingsFailed"] = "保存设置失败！",
			["Message.ResetTitle"] = "重置？",
			["Message.ResetConfirm"] = "您真的要重置您的设置吗？",
			["Message.GameDirNotExist"] = "选定的Helldivers 2文件夹不存在！",
			["Message.GameDirInvalid"] = "选定的Helldivers 2文件夹不在有效目录中！",
			["Message.GameDirNoData"] = "选定的Helldivers 2根路径不包含名为data的目录！",
			["Message.GameDirNoTools"] = "选定的Helldivers 2根路径不包含名为tools的目录！",
			["Message.GameDirNoBin"] = "选定的Helldivers 2根路径不包含名为bin的目录！",
			["Message.GameDirNoExe"] = "选定的Helldivers 2路径在名为bin的文件夹中不包含名为helldivers2.exe的文件！",
			["Message.FileNamePrompt"] = "文件名？",
			["Message.FileNameDesc"] = "请输入您要跳过补丁0的存档文件的16个字符名称。",
			["Message.FileNameLengthError"] = "存档文件名只能是16个字符长。",
			["Message.LookingForGame"] = "正在查找游戏",
			["Message.GameNotFound"] = "无法自动找到您的Helldivers 2游戏。请手动设置。",
			
			// Dialog Titles
			["Dialog.SelectGameFolder"] = "请选择您的Helldivers 2文件夹...",
			["Dialog.SelectStorageFolder"] = "请选择一个文件夹，让这个管理器存储其模组...",
			["Dialog.SelectTempFolder"] = "请选择一个您想让这个管理器用于临时文件的文件夹...",
		};

		var zhCnPath = Path.Combine(s_languageDir.FullName, "zh-cn.json");
		await File.WriteAllTextAsync(zhCnPath, JsonSerializer.Serialize(zhCnTranslations, new JsonSerializerOptions { WriteIndented = true }));
		
		_logger.LogInformation("Created default language files");
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
