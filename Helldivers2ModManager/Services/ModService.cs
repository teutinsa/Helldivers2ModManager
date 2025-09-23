using Helldivers2ModManager.Exceptions;
using Helldivers2ModManager.Extensions;
using Helldivers2ModManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpCompress;
using SharpCompress.Archives;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Transient)]
internal sealed partial class ModService
{
	private readonly struct PatchFileTriplet
	{
		public FileInfo? Patch { get; init; }

		public FileInfo? GpuResources { get; init; }

		public FileInfo? Stream { get; init; }
	}

	[MemberNotNullWhen(true, nameof(_settingsService))]
	public bool Initialized { get; private set; }

	public IReadOnlyList<ModData> Mods => _mods;

	public event Action<ModData>? ModAdded;

	public event Action<ModData>? ModRemoved;

	private readonly ILogger<ModService> _logger;
	private readonly List<ModData> _mods;
	private SettingsService? _settingsService;

	public ModService(ILogger<ModService> logger)
	{
		_logger = logger;
		_mods = new();
	}
	
	public ModProblem[] Init(SettingsService settings)
	{
		if (Initialized)
			throw new InvalidOperationException("Object already initialized!");

		if (!settings.Validate())
			throw new ArgumentException("Settings are invalid!", nameof(settings));

		var problems = new List<ModProblem>();

		_settingsService = settings;
		_logger.LogInformation("Initializing mod service");

		var modsDir = new DirectoryInfo(Path.Combine(_settingsService.StorageDirectory, "Mods"));

		_logger.LogDebug("Checking \"Mods\" directroy existance");
		if (modsDir.Exists)
			_logger.LogDebug("Found \"Mods\" directory");
		else
		{
			_logger.LogDebug("Creating \"Mods\" directory");
			modsDir.Create();
		}

		var dirs = modsDir.GetDirectories();
		_logger.LogInformation("Found {} folders in \"Mods\" directory", dirs.Length);

		foreach (var dir in dirs)
		{
			_logger.LogDebug("Processing \"{}\"", dir.FullName);

			_logger.LogDebug("Checking for \"manifest.json\"");
			var manifestFile = new FileInfo(Path.Combine(dir.FullName, "manifest.json"));
			if (manifestFile.Exists)
			{
				IModManifest manifest;

				try
				{
					_logger.LogDebug("Parsing manifest");
					manifest = ModManifest.DeserializeFromFile(manifestFile);
				}
				catch (UnknownManifestVersionException)
				{
					_logger.LogError("Manifest \"{}\" has unknown", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.UnknownManifestVersion,
					});
					continue; // skip
				}
				catch (EndOfLifeException)
				{
					_logger.LogError("Manifest \"{}\" is unsupported version 2", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.OutOfSupportManifest,
					});
					continue; // skip
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Unable to parse manifest \"{}\"", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.CantParseManifest,
					});
					continue; // skip
				}

				if (_mods.Any(data => data.Manifest.Guid == manifest.Guid))
				{
					_logger.LogError("Mod \"{}\" has a duplicate guid of \"{}\"", dir.FullName, manifest.Guid);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.Duplicate,
					});
					continue; // skip
				}

				if (!CheckPaths(manifest, problems, dir, manifestFile))
					continue;

				_mods.Add(new ModData(dir, manifest));
			}
			else
			{
				_logger.LogWarning("No manifest found in \"{}\", deleting", dir.FullName);
				problems.Add(new ModProblem
				{
					Directory = dir,
					Kind = ModProblemKind.NoManifestFound,
				});
				dir.Delete(true);
			}
		}

		Initialized = true;
		_logger.LogInformation("Loaded {} mods", _mods.Count);
		_logger.LogInformation("Mod service initialization complete");
		return problems.ToArray();
	}
	
	public async Task<ModProblem[]> TryAddModFromArchiveAsync(FileInfo file)
	{
		GuardInitialized();

		var problems = new List<ModProblem>();

		_logger.LogInformation("Attempting to add mod from \"{}\"", file.Name);

		var tmpDir = new DirectoryInfo(Path.Combine(_settingsService.TempDirectory, file.Name[..^file.Extension.Length]));
		_logger.LogInformation("Creating clean temporary directory \"{}\"", tmpDir.FullName);
		if (tmpDir.Exists)
			tmpDir.Delete(true);
		tmpDir.Create();

		_logger.LogInformation("Extracting archive");
		await Task.Run(() => ArchiveFactory.Open(file).ExtractToDirectory(tmpDir.FullName));

		var manifestFile = new FileInfo(Path.Combine(tmpDir.FullName, "manifest.json"));

		IModManifest manifest;
		if (manifestFile.Exists)
		{
			manifest = ModManifest.DeserializeFromFile(manifestFile);

			if (!CheckPaths(manifest, problems, tmpDir, manifestFile))
			{
				tmpDir.Delete(true);
				return problems.ToArray();
			}
		}
		else
		{
			problems.Add(new ModProblem
			{
				Directory = tmpDir,
				Kind = ModProblemKind.NoManifestFound,
			});
			manifest = ModManifest.InferFromDirectory(tmpDir);

			var stream = manifestFile.Open(FileMode.CreateNew, FileAccess.Write, FileShare.Read);
			var writer = new Utf8JsonWriter(stream);

			manifest.Serialize(writer);

			await writer.DisposeAsync();
			await stream.DisposeAsync();
		}

		_logger.LogInformation("Moving mod to storage");
		var modDir = new DirectoryInfo(Path.Combine(_settingsService.StorageDirectory, "Mods", manifest.Name));
		if (modDir.Exists)
		{
			_logger.LogError("Mod directory already exists in storage");
			tmpDir.Delete(true);
			problems.Add(new ModProblem
			{
				Directory = modDir,
				Kind = ModProblemKind.Duplicate,
			});
			return problems.ToArray();
		}
		modDir.Parent?.Create();
		await Task.Run(() => tmpDir.CopyTo(modDir.FullName));

		_logger.LogInformation("Adding mod");
		var mod = new ModData(modDir, manifest);
		_mods.Add(mod);
		ModAdded?.Invoke(mod);

		tmpDir.Delete(true);
		return problems.ToArray();
	}

	public async Task RemoveAsync(ModData mod)
	{
		GuardInitialized();

		_logger.LogInformation("Attempting to remove {}", mod.Manifest.Guid);

		if (!_mods.Remove(mod))
		{
			_logger.LogInformation("Removal unsuccessful");
			return;
		}

		ModRemoved?.Invoke(mod);

		await Task.Run(() => mod.Directory.Delete(true));

		_logger.LogInformation("Mod {} removed", mod.Manifest.Name);
	}

	public async Task DeployAsync(Guid[] modGuids)
	{
		GuardInitialized();

		if (modGuids.Length == 0)
		{
			_logger.LogInformation("No mods enabled, skipping deployment");
			return;
		}

		await PurgeAsync();

		_logger.LogInformation("Starting deployment of {} mods", modGuids.Length);

		var stageDir = new DirectoryInfo(Path.Combine(_settingsService.TempDirectory, "Staging"));
		_logger.LogInformation("Creating clean staging directory \"{}\"", stageDir.FullName);
		if (stageDir.Exists)
			stageDir.Delete(true);
		stageDir.Create();

		var groups = new Dictionary<string, List<PatchFileTriplet>>();

		void AddFilesFromDir(DirectoryInfo dir)
		{
			var files = dir.GetFiles().Where(static f => GetPatchFileRegex().IsMatch(f.Name)).ToArray();

			foreach (var file in files)
				_logger.LogDebug("Adding file \"{}\"", file.FullName);

			var names = new HashSet<string>();
			for (int i = 0; i < files.Length; i++)
				names.Add(files[i].Name[0..16]);

			foreach (var name in names)
			{
				var indexes = new HashSet<int>();
				foreach (var file in files)
				{
					var match = GetPatchIndexRegex().Match(file.Name);
					indexes.Add(int.Parse(match.Groups[1].ValueSpan));
				}

				foreach (var index in indexes)
				{
					FileInfo? patchFile = files.FirstOrDefault(f => Regex.IsMatch(f.Name, @$"^{name}\.patch_{index}$"));
					FileInfo? gpuFile = files.FirstOrDefault(f => Regex.IsMatch(f.Name, @$"^{name}\.patch_{index}.gpu_resources$"));
					FileInfo? streamFile = files.FirstOrDefault(f => Regex.IsMatch(f.Name, @$"^{name}\.patch_{index}.stream$"));

					if (!groups.ContainsKey(name))
						groups.Add(name, []);
					groups[name].Add(new PatchFileTriplet
					{
						Patch = patchFile,
						GpuResources = gpuFile,
						Stream = streamFile
					});
				}
			}
		}

		_logger.LogInformation("Grouping files");
		foreach (var guid in modGuids)
		{
			var mod = GetModByGuid(guid);
			if (mod is null)
			{
				_logger.LogWarning("Mod with guid {} not found, skipping", guid);
				continue;
			}

			_logger.LogInformation("Working on \"{}\"", mod.Manifest.Name);

			switch (mod.Manifest.Version)
			{
				case ManifestVersion.Legacy:
				{
					_logger.LogInformation("Mod \"{}\" has legacy manifest", mod.Manifest.Name);

					var man = (LegacyModManifest)mod.Manifest;
					var enabled = mod.EnabledOptions;
					var selected = mod.SelectedOptions;

					if (man.Options is not null)
					{
						if (selected is not int[] { Length: 1 })
						{
							_logger.LogError("Options have the wrong count");
							continue;
						}

						var dir = new DirectoryInfo(Path.Combine(mod.Directory.FullName, man.Options[selected[0]]));
						AddFilesFromDir(dir);
					}
					else
						AddFilesFromDir(mod.Directory);
				}
				break;

				case ManifestVersion.V1:
				{
					_logger.LogInformation("Mod \"{}\" has V1 manifest", mod.Manifest.Name);

					var man = (V1ModManifest)mod.Manifest;
					var enabled = mod.EnabledOptions;
					var selected = mod.SelectedOptions;

					if (man.Options is not null)
					{
						if (enabled.Length != man.Options.Count)
						{
							_logger.LogError("Enabled option counts are not equal");
							continue;
						}

						if (selected.Length != man.Options.Count)
						{
							_logger.LogError("Selected option counts are not equal");
							continue;
						}

						_logger.LogInformation("Making include list");
						for (int i = 0; i < enabled.Length; i++)
						{
							if (!enabled[i])
								continue;

							var opt = man.Options[i];

							if (opt.Include is { } incs)
								foreach (var inc in incs)
								{
									var dir = new DirectoryInfo(Path.Combine(mod.Directory.FullName, inc));
									_logger.LogInformation("Adding \"{}\"", dir.FullName);
									AddFilesFromDir(dir);
								}

							if (opt.SubOptions is { } subs)
							{
								var sub = subs[selected[i]];
								foreach (var inc in sub.Include)
								{
									var dir = new DirectoryInfo(Path.Combine(mod.Directory.FullName, inc));
									_logger.LogInformation("Adding \"{}\"", dir.FullName);
									AddFilesFromDir(dir);
								}
							}
						}
					}
					else
						AddFilesFromDir(mod.Directory);
				}
				break;

				default:
					throw new NotSupportedException("Unknown manifest version!");
			}
		}

		_logger.LogInformation("Copying files");
		foreach (var (name, list) in groups)
		{
			int offset = 0;
			if (_settingsService.SkipList.Contains(name))
				offset = 1;

			for (int i = 0; i < list.Count; i++)
			{
				var triplet = list[i];
				var index = i + offset;

				var newPatchPath = Path.Combine(_settingsService.GameDirectory, "data", $"{name}.patch_{index}");
				FileInfo pathDest;
				if (triplet.Patch is not null)
				{
					pathDest = triplet.Patch.CopyTo(newPatchPath);
				}
				else
				{
					pathDest = new FileInfo(newPatchPath);
					pathDest.Create().Dispose();
				}

				var newGpuResourcesPath = Path.Combine(_settingsService.GameDirectory, "data", $"{name}.patch_{index}.gpu_resources");
				FileInfo gpuResourceDest;
				if (triplet.GpuResources is not null)
				{
					gpuResourceDest = triplet.GpuResources.CopyTo(newGpuResourcesPath);
				}
				else
				{
					gpuResourceDest = new FileInfo(newGpuResourcesPath);
					gpuResourceDest.Create().Dispose();
				}

				var newStreamPath = Path.Combine(_settingsService.GameDirectory, "data", $"{name}.patch_{index}.stream");
				FileInfo streamDest;
				if (triplet.Stream is not null)
				{
					streamDest = triplet.Stream.CopyTo(newStreamPath);
				}
				else
				{
					streamDest = new FileInfo(newStreamPath);
					streamDest.Create().Dispose();
				}
			}
		}

		_logger.LogInformation("Deployment success");
	}

	public async Task PurgeAsync()
	{
		GuardInitialized();

		_logger.LogInformation("Purging mods");

		var dataDir = new DirectoryInfo(Path.Combine(_settingsService.GameDirectory, "data"));

		var files = dataDir.GetFiles("*.patch_*");
		_logger.LogDebug("Found {} patch files", files.Length);

		var tasks = new List<Task>();
		foreach (var file in files)
		{
			var task = Task.Run(() =>
			{
				_logger.LogTrace("Attempting to delete \"{}\"", file.Name);
				file.Delete();
				_logger.LogTrace("Deleted \"{}\"", file.Name);
			});
			tasks.Add(task);
		}

		await Task.WhenAll(tasks);

		_logger.LogInformation("Purge complete");
	}

	public ModData? GetModByGuid(Guid guid)
	{
		foreach (var mod in _mods)
			if (mod.Manifest.Guid == guid)
				return mod;
		return null;
	}

	[MemberNotNull(nameof(_settingsService))]
	private void GuardInitialized()
	{
		if (!Initialized)
			throw new InvalidOperationException("Object not initialized!");
	}

	private bool CheckPaths(IModManifest manifest, List<ModProblem> problems, DirectoryInfo dir, FileInfo manifestFile)
	{
		bool error = false;

		_logger.LogDebug("Checking manifest paths");
		
		switch (manifest)
		{
			case LegacyModManifest { Options: { } opts } man:
			{
				if (opts.Count == 0)
				{
					_logger.LogWarning("Empty Options found in manifest \"{}\"", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.EmptyOptions,
					});
				}

				if (man.IconPath is not null && !File.Exists(Path.Combine(dir.FullName, man.IconPath)))
				{
					error = true;
					_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, man.IconPath);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.InvalidPath,
						ExtraData = man.IconPath,
					});
				}

				foreach (var opt in opts)
					if (!Directory.Exists(Path.Combine(dir.FullName, opt)))
					{
						error = true;
						_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, opt);
						problems.Add(new ModProblem
						{
							Directory = dir,
							Kind = ModProblemKind.InvalidPath,
							ExtraData = opt,
						});
					}
				break;
			}

			case V1ModManifest { Options: { } opts } man:
			{
				if (opts.Count == 0)
				{
					_logger.LogWarning("Empty Options found in manifest \"{}\"", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.EmptyOptions,
					});
				}

				if (opts.Any(static opt => opt.SubOptions is { Count: 0 }))
				{
					_logger.LogWarning("Empty Sub-Options found in manifest \"{}\"", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.EmptySubOptions,
					});
				}

				if (opts.Any(static opt => opt.SubOptions?.Any(static sub => sub.Include.Count == 0) ?? false))
				{
					_logger.LogWarning("Empty includes found in manifest \"{}\"", manifestFile.FullName);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.EmptyIncludes,
					});
				}

				if (man.IconPath is not null && !File.Exists(Path.Combine(dir.FullName, man.IconPath)))
				{
					error = true;
					_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, man.IconPath);
					problems.Add(new ModProblem
					{
						Directory = dir,
						Kind = ModProblemKind.InvalidPath,
						ExtraData = man.IconPath,
					});
				}

				foreach (var opt in opts)
				{
					if (opt.Image is not null && !File.Exists(Path.Combine(dir.FullName, opt.Image)))
					{
						error = true;
						_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, opt.Image);
						problems.Add(new ModProblem
						{
							Directory = dir,
							Kind = ModProblemKind.InvalidPath,
							ExtraData = opt.Image,
						});
					}

					if (opt.Include is not null)
						foreach (var inc in opt.Include)
							if (!Directory.Exists(Path.Combine(dir.FullName, inc)))
							{
								error = true;
								_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, inc);
								problems.Add(new ModProblem
								{
									Directory = dir,
									Kind = ModProblemKind.InvalidPath,
									ExtraData = inc,
								});
							}

					if (opt.SubOptions is not null)
						foreach (var sub in opt.SubOptions)
						{
							if (sub.Image is not null && !File.Exists(Path.Combine(dir.FullName, sub.Image)))
							{
								error = true;
								_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, sub.Image);
								problems.Add(new ModProblem
								{
									Directory = dir,
									Kind = ModProblemKind.InvalidPath,
									ExtraData = sub.Image,
								});
							}

							foreach (var inc in sub.Include)
								if (!Directory.Exists(Path.Combine(dir.FullName, inc)))
								{
									error = true;
									_logger.LogError("Manifest \"{}\" contains invalid path \"{}\"", manifestFile.FullName, inc);
									problems.Add(new ModProblem
									{
										Directory = dir,
										Kind = ModProblemKind.InvalidPath,
										ExtraData = inc,
									});
								}
						}
				}
				break;
			}
		}

		_logger.LogDebug("Path check complete");

		return !error;
	}

	[GeneratedRegex(@"^[a-z0-9]{16}\.patch_[0-9]+(\.(stream|gpu_resources))?$")]
	private static partial Regex GetPatchFileRegex();

	[GeneratedRegex(@"\.patch_[0-9]+")]
	private static partial Regex GetPatchRegex();

	[GeneratedRegex(@"^(?:[a-z0-9]{16}\.patch_)([0-9]+)(?:(?:\.(?:stream|gpu_resources))?)$")]
	private static partial Regex GetPatchIndexRegex();
}