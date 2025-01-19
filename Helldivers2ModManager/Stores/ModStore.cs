// Ignore Spelling: Gpu Guids

using Helldivers2ModManager.Exceptions;
using Helldivers2ModManager.Extensions;
using Helldivers2ModManager.Models;
using Helldivers2ModManager.Services;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;

namespace Helldivers2ModManager.Stores;

internal sealed class ModEventArgs(ModData mod) : EventArgs
{
	public ModData Mod { get; } = mod;
}

internal delegate void ModEventHandler(object sender, ModEventArgs e);

internal sealed partial class ModStore
{
	public readonly struct PatchFileTriplet
	{
		public FileInfo? Patch { get; init; }

		public FileInfo? GpuResources { get; init; }

		public FileInfo? Stream { get; init; }
	}

	public IReadOnlyList<ModData> Mods => _mods;

	public event ModEventHandler? ModAdded;
	public event ModEventHandler? ModRemoved;

	private readonly ILogger<ModStore> _logger;
	private readonly SettingsStore _settingsStore;
	private readonly List<ModData> _mods;
	private readonly IModManifestService _manifestService;

	public ModStore(ILogger<ModStore> logger, SettingsStore settingsStore, IModManifestService manifestService)
	{
		_logger = logger;
		_settingsStore = settingsStore;
		_manifestService = manifestService;

		_logger.LogInformation("Retrieving mods for startup");
		var modDir = new DirectoryInfo(Path.Combine(_settingsStore.StorageDirectory, "Mods"));
		if (modDir.Exists)
		{
			var dirs = modDir.GetDirectories();
			var tasks = new Task<object?>[dirs.Length];
			for (int i = 0; i < tasks.Length; i++)
			{
				try
				{
					var dirName = dirs[i].Name;
					var file = dirs[i].GetFiles("manifest.json").FirstOrDefault();
					if (file is null)
					{
						tasks[i] = Task.FromResult<object?>(null);
						_logger.LogWarning("No manifest found in \"{}\"", dirs[i].FullName);
					}
					else
						tasks[i] = Task.Run(async () =>
						{
							try
							{
								return await _manifestService.FromFileAsync(file);
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Error during manifest reading of \"{}\"", dirName);
								return null;
							}
						});
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error during mod store initialization");
					tasks[i] = Task.FromResult<object?>(null);
				}
			}

			var manifests = Task.WhenAll(tasks).GetAwaiter().GetResult();
			_mods = new(manifests.Length);

			for (int i = 0; i < manifests.Length; i++)
			{
				var dir = dirs[i];
				var man = manifests[i];
				if (man is not null)
					_mods.Add(new ModData(dirs[i], new ModManifest(man)));
				else
				{
					_logger.LogWarning("Skipping \"{}\"", dir.Name);
				}
			}
		}
		else
		{
			_mods = [];
			_logger.LogInformation("Mod directory does not exist yet");
		}
	}

	/// <summary>
	/// Attempts to add an archive file as a mod.
	/// </summary>
	/// <param name="file">The archive file to add as a mod.</param>
	/// <returns><see langword="true"/> if mod is successfully added, otherwise <see langword="false"/>.</returns>
	public async Task<bool> TryAddModFromArchiveAsync(FileInfo file)
	{
		_logger.LogInformation("Attempting to add mod from \"{}\"", file.Name);

		var tmpDir = new DirectoryInfo(Path.Combine(_settingsStore.TempDirectory, file.Name[..^file.Extension.Length]));
		_logger.LogInformation("Creating clean temporary directory \"{}\"", tmpDir.FullName);
		if (tmpDir.Exists)
			tmpDir.Delete(true);
		tmpDir.Create();

		_logger.LogInformation("Extracting archive");
		await Task.Run(() => ArchiveFactory.Open(file.FullName).ExtractToDirectory(tmpDir.FullName));

		var man = await _manifestService.FromDirectoryAsync(tmpDir);

		if (man is null)
			return false;

		await _manifestService.ToFileAsync(man, new(Path.Combine(tmpDir.FullName, "manifest.json")));

		var manifest = new ModManifest(man);

		_logger.LogInformation("Moving mod to storage");
		var modDir = new DirectoryInfo(Path.Combine(_settingsStore.StorageDirectory, "Mods", manifest.Name));
		if (modDir.Exists)
		{
			_logger.LogError("Mod directory already exists in storage");
			tmpDir.Delete(true);
			return false;
		}
		modDir.Parent?.Create();
		await Task.Run(() => tmpDir.CopyTo(modDir.FullName));

		_logger.LogInformation("Adding mod");
		var mod = new ModData(modDir, manifest);
		_mods.Add(mod);
		OnModAdded(new ModEventArgs(mod));

		tmpDir.Delete(true);
		return true;
	}

	/// <summary>
	/// Retrieves a mod by its global unique identifier.
	/// </summary>
	/// <param name="guid">The <see cref="Guid"/> to look for.</param>
	/// <returns>A <see cref="ModData"/> object if found, otherwise <see langword="null"/>.</returns>
	public ModData? GetModByGuid(Guid guid)
	{
		return _mods.FirstOrDefault(m => m.Manifest.Guid == guid);
	}

	/// <summary>
	/// Attempts to remove a mod.
	/// </summary>
	/// <param name="mod">The mod to remove.</param>
	/// <returns><see langword="true"/> if the removal was successful, otherwise <see langword="false"/>.</returns>
	public bool Remove(ModData mod)
	{
		_logger.LogInformation("Attempting to remove {}", mod.Manifest.Guid);
		if (_mods.Remove(mod))
		{
			mod.Directory.Delete(true);
			OnModRemoved(new ModEventArgs(mod));
			_logger.LogInformation("Mod \"{}\" removed", mod.Manifest.Name);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Deploys all mods listed by <paramref name="modGuids"/>.
	/// </summary>
	/// <param name="modGuids">The mods <see cref="Guid"/>s to deploy.</param>
	/// <exception cref="InvalidOperationException">Thrown if the Helldivers 2 path is not set.</exception>
	/// <exception cref="NotSupportedException">Thrown if the manifest version is unknown.</exception>
	/// <exception cref="DeployException">Thrown if any other error happens during deployment.</exception>
	public async Task DeployAsync(Guid[] modGuids)
	{
		if (string.IsNullOrEmpty(_settingsStore.GameDirectory))
		{
			_logger.LogError("Helldivers 2 path not set!");
			throw new InvalidOperationException("Helldivers 2 path not set!");
		}

		if (modGuids.Length == 0)
		{
			_logger.LogInformation("No mods enabled, skipping deployment");
			return;
		}

		try
		{
			await PurgeAsync();
		}
		catch (PurgeException ex)
		{
			throw new DeployException(ex);
		}

		_logger.LogInformation("Starting deployment of {} mods", modGuids.Length);

		try
		{
			var stageDir = new DirectoryInfo(Path.Combine(_settingsStore.TempDirectory, "Staging"));
			_logger.LogInformation("Creating clean staging directory \"{}\"", stageDir.FullName);
			if (stageDir.Exists)
				stageDir.Delete(true);
			stageDir.Create();
		}
		catch (UnauthorizedAccessException ex)
		{
			throw new DeployException(ex);
		}
		catch (DirectoryNotFoundException ex)
		{
			throw new DeployException(ex);
		}
		catch (SecurityException ex)
		{
			throw new DeployException(ex);
		}
		catch (IOException ex)
		{
			throw new DeployException(ex);
		}

		var groups = new Dictionary<string, List<PatchFileTriplet>>();

		void AddFilesFromDir(DirectoryInfo dir)
		{
			try
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
			catch (DirectoryNotFoundException ex)
			{
				throw new AddFilesException(ex);
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

			try
			{
				_logger.LogInformation("Working on \"{}\"", mod.Manifest.Name);

				switch (mod.Manifest.Version)
				{
					case ModManifest.ManifestVersion.Legacy:
						{
							_logger.LogInformation("Mod \"{}\" has legacy manifest", mod.Manifest.Name);

							var man = mod.Manifest.Legacy;
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

					case ModManifest.ManifestVersion.V1:
						{
							_logger.LogInformation("Mod \"{}\" has V1 manifest", mod.Manifest.Name);

							var man = mod.Manifest.V1;
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

									if (opt.Include is string[] incs)
										foreach (var inc in incs)
										{
											var dir = new DirectoryInfo(Path.Combine(mod.Directory.FullName, inc));
											_logger.LogInformation("Adding \"{}\"", dir.FullName);
											AddFilesFromDir(dir);
										}

									if (opt.SubOptions is ModSubOption[] subs)
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

					case ModManifest.ManifestVersion.Unknown:
						throw new NotSupportedException("Unknown manifest version!");
				}
			}
			catch (AddFilesException ex)
			{
				throw new DeployException(mod, ex);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DeployException(mod, ex);
			}
		}

		_logger.LogInformation("Copying files");
		foreach (var (name, list) in groups)
		{
			int offset = 0;
			if (_settingsStore.SkipList.Contains(name))
				offset = 1;

			for (int i = 0; i < list.Count; i++)
			{
				var triplet = list[i];
				var index = i + offset;

				try
				{
					var newPatchPath = Path.Combine(_settingsStore.GameDirectory, "data", $"{name}.patch_{index}");
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

					var newGpuResourcesPath = Path.Combine(_settingsStore.GameDirectory, "data", $"{name}.patch_{index}.gpu_resources");
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

					var newStreamPath = Path.Combine(_settingsStore.GameDirectory, "data", $"{name}.patch_{index}.stream");
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
				catch (SecurityException ex)
				{
					throw new DeployException(triplet, ex);
				}
				catch (UnauthorizedAccessException ex)
				{
					throw new DeployException(triplet, ex);
				}
				catch (PathTooLongException ex)
				{
					throw new DeployException(triplet, ex);
				}
				catch (DirectoryNotFoundException ex)
				{
					throw new DeployException(triplet, ex);
				}
				catch (IOException ex)
				{
					throw new DeployException(triplet, ex);
				}
				catch (NotSupportedException ex)
				{
					throw new DeployException(triplet, ex);
				}
			}
		}

		_logger.LogInformation("Deployment success");
	}

	public async Task PurgeAsync()
	{
		/*
		_logger.LogInformation("Purging mods");
		var path = Path.Combine(_settingsStore.StorageDirectory, "installed.txt");

		if (File.Exists(path))
		{
			_logger.LogInformation("Reading installed file list");
			var installedFiles = await File.ReadAllLinesAsync(path);

			_logger.LogInformation("Deleting files");
			foreach (var file in installedFiles)
				if (File.Exists(file))
					File.Delete(file);

			_logger.LogInformation("Deleting installed file list");
			File.Delete(path);
		}

		_logger.LogInformation("Purge complete");
		*/

		_logger.LogInformation("Purging mods");

		await Task.Run(() =>
		{
			try
			{
				var dataDir = new DirectoryInfo(Path.Combine(_settingsStore.GameDirectory, "data"));

				var files = dataDir.EnumerateFiles("*.patch_*").ToArray();
				_logger.LogDebug("Found {} patch files", files.Length);

				foreach (var file in files)
				{
					_logger.LogTrace("Deleting \"{}\"", file.Name);
					if (file.Exists)
					{
						try
						{
							file.Delete();
						}
						catch(IOException ex)
						{
							throw new PurgeException(file, ex);
						}
						catch (SecurityException ex)
						{
							throw new PurgeException(file, ex);
						}
						catch (UnauthorizedAccessException ex)
						{
							throw new PurgeException(file, ex);
						}
					}
				}

				_logger.LogInformation("Purge complete");
			}
			catch(DirectoryNotFoundException ex)
			{
				throw new PurgeException(ex);
			}
			catch (SecurityException ex)
			{
				throw new PurgeException(ex);
			}
		});
	}

	private void OnModAdded(ModEventArgs e)
	{
		ModAdded?.Invoke(this, e);
	}

	private void OnModRemoved(ModEventArgs e)
	{
		ModRemoved?.Invoke(this, e);
	}

	[GeneratedRegex(@"^[a-z0-9]{16}\.patch_[0-9]+(\.(stream|gpu_resources))?$")]
	private static partial Regex GetPatchFileRegex();

	[GeneratedRegex(@"\.patch_[0-9]+")]
	private static partial Regex GetPatchRegex();

	[GeneratedRegex(@"^(?:[a-z0-9]{16}\.patch_)([0-9]+)(?:(?:\.(?:stream|gpu_resources))?)$")]
	private static partial Regex GetPatchIndexRegex();
}
