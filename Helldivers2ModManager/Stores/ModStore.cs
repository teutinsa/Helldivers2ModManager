using Helldivers2ModManager.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using SharpCompress.Archives;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Helldivers2ModManager.Stores
{
	internal sealed class ModEventArgs(ModData mod) : EventArgs
	{
		public ModData Mod { get; } = mod;
	}

	internal delegate void ModEventHandler(object sender, ModEventArgs e);

	internal sealed partial class ModStore
	{
		private readonly struct PatchFileTriplet
		{
			public FileInfo? Patch { get; init; }

			public FileInfo? GpuResources { get; init; }

			public FileInfo? Stream { get; init; }
		}

		public IReadOnlyList<ModData> Mods => _mods;

		public event ModEventHandler? ModAdded;
		public event ModEventHandler? ModRemoved;

		private static readonly JsonSerializerOptions s_jsonOptions = new()
		{
			AllowTrailingCommas = true,
			WriteIndented = true,
			ReadCommentHandling = JsonCommentHandling.Skip
		};
		private readonly ILogger<ModStore> _logger;
		private readonly SettingsStore _settingsStore;
		private readonly List<ModData> _mods;

		public ModStore(ILogger<ModStore> logger, SettingsStore settingsStore)
		{
			_logger = logger;
			_settingsStore = settingsStore;
			_mods = [];

			_logger.LogInformation("Retrieving mods for startup");
			var modDir = new DirectoryInfo(Path.Combine(_settingsStore.StorageDirectory, "Mods"));
			if (modDir.Exists)
			{
				foreach (var dir in modDir.GetDirectories())
				{
					var manifestFile = dir.GetFiles("manifest.json").FirstOrDefault();
					if (manifestFile is null)
					{
						_logger.LogWarning("No manifest found in \"{}\"", dir.FullName);
						_logger.LogWarning("Skipping \"{}\"", dir.Name);
						continue;
					}

					try
					{
						var manifest = ModManifest.Deserialize(manifestFile);
						if (manifest is null)
						{
							_logger.LogWarning("Unable to parse manifest \"{}\"", manifestFile.FullName);
							_logger.LogWarning("Skipping \"{}\"", dir.Name);
							continue;
						}

						_mods.Add(new ModData(dir, manifest));
					}
					catch (JsonException ex)
					{
						_logger.LogWarning(ex, "An Exception occurred while parsing manifest \"{}\"", manifestFile.FullName);
						_logger.LogWarning("Skipping \"{}\"", dir.Name);
						continue;
					}
				}
			}
			else
				_logger.LogInformation("Mod directory does not exist yet");
		}

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

			var subDirs = tmpDir.GetDirectories();
			var rootFiles = tmpDir.GetFiles();
			var dirNames = subDirs.Select(static dir => dir.Name).ToArray();

			_logger.LogInformation("Looking for manifest");
			ModManifest? manifest;
			int option = -1;
			if (rootFiles.Where(static f => f.Name == "manifest.json").FirstOrDefault() is FileInfo manifestFile)
			{
				_logger.LogInformation("Deserializing found manifest");
				manifest = ModManifest.Deserialize(manifestFile);
				if (manifest is null)
				{
					_logger.LogError("Deserialization failed");
					tmpDir.Delete(true);
					return false;
				}

				if (!IsGuidFree(manifest.Guid))
				{
					_logger.LogError("Manifest guid {} is already taken", manifest.Guid);
					tmpDir.Delete(true);
					return false;
				}

				if (manifest.Options is not null)
				{
					option = 0;

					if (manifest.Options.Count == 0)
					{
						_logger.LogError("Options where empty");
						tmpDir.Delete(true);
						return false;
					}

					if (manifest.Options.Distinct().Count() != manifest.Options.Count)
					{
						_logger.LogError("Options contain duplicates");
						tmpDir.Delete(true);
						return false;
					}

					var opts = new HashSet<string>(manifest.Options);
					var dirs = new HashSet<string>(dirNames);
					if(!opts.IsSubsetOf(dirs))
					{
						_logger.LogError("Options and sub-directories mismatch");
						tmpDir.Delete(true);
						return false;
					}
				}
			}
			else
			{
				_logger.LogInformation("No manifest found");
				_logger.LogInformation("Attempting to infer manifest from directory structure");

				string[]? options;
				if (subDirs.Length > 0)
				{
					_logger.LogInformation("Found {} sub-directories that will be added as options", subDirs.Length);
					options = dirNames;
					option = 0;
				}
				else
				{
					_logger.LogInformation("No sub-directories found");
					options = null;
				}

				_logger.LogInformation("Writing generate manifest");
				manifest = new ModManifest
				{
					Guid = GetFreeGuid(),
					Name = file.Name[..^file.Extension.Length],
					Description = "Locally imported mod",
					Options = options
				};
				var genManifest = new FileInfo(Path.Combine(tmpDir.FullName, "manifest.json"));
				manifest.Serialize(genManifest);
			}

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
			var mod = new ModData(modDir, manifest) { Option = option };
			_mods.Add(mod);
			OnModAdded(new ModEventArgs(mod));

			tmpDir.Delete(true);
			return true;
		}

		public ModData? GetModByGuid(Guid guid)
		{
			return _mods.FirstOrDefault(m => m.Manifest.Guid == guid);
		}

		public bool Remove(ModData mod)
		{
			_logger.LogInformation("Attempting to remove {}", mod.Manifest.Guid);
			if (_mods.Remove(mod))
			{
				mod.Directory.Delete(true);
				OnModRemoved(new ModEventArgs(mod));
				_logger.LogInformation("Mod {} removed", mod.Manifest.Guid);
				return true;
			}
			return false;
		}

		public async Task<bool> DeployAsync(Guid[] modGuids)
		{
			if (string.IsNullOrEmpty(_settingsStore.GameDirectory))
			{
				_logger.LogError("Helldivers 2 path not set!");
				return false;
			}

			if (modGuids.Length == 0)
			{
				_logger.LogInformation("No mods enabled, skipping deployment");
				return true;
			}

			await PurgeAsync();

			_logger.LogInformation("Starting deployment of {} mods", modGuids.Length);

			var stageDir = new DirectoryInfo(Path.Combine(_settingsStore.TempDirectory, "Staging"));
			_logger.LogInformation("Creating clean staging directory \"{}\"", stageDir.FullName);
			if (stageDir.Exists)
				stageDir.Delete(true);
			stageDir.Create();

			var groups = new Dictionary<string, List<PatchFileTriplet>>();

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

				_logger.LogInformation("Looking for option");
				DirectoryInfo modDir;
				if (mod.Option == -1)
				{
					modDir = mod.Directory;
					_logger.LogInformation("No options found using root");
				}
				else
				{
					modDir = new DirectoryInfo(Path.Combine(mod.Directory.FullName, mod.Manifest.Options![mod.Option]));
					_logger.LogInformation("Option \"{}\" selected", modDir.Name);
				}

				var files = modDir.GetFiles().Where(static f => GetParchFileRegex().IsMatch(f.Name)).ToArray();
				_logger.LogInformation("Found {} files", files.Length);
				var names = new HashSet<string>();
				for (int i = 0; i < files.Length; i++)
					names.Add(files[i].Name[0..16]);
				_logger.LogInformation("Grouped into {}", names.Count);

				foreach (var name in names)
				{
					FileInfo? patchFile = files.FirstOrDefault(f => Regex.IsMatch(f.Name, @$"^{name}\.patch_[0-9]+$"));
					FileInfo? gpuFile = files.FirstOrDefault(f => Regex.IsMatch(f.Name, @$"^{name}\.patch_[0-9]+.gpu_resources$"));
					FileInfo? streamFile = files.FirstOrDefault(f => Regex.IsMatch(f.Name, @$"^{name}\.patch_[0-9]+.stream$"));

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

			_logger.LogInformation("Copying files");
			var installedFiles = new List<string>();
			foreach (var (name, list) in groups)
			{
				for (int i = 0; i < list.Count; i++)
				{
					var triplet = list[i];
					if (triplet.Patch is not null)
					{
						var dest = triplet.Patch.CopyTo(Path.Combine(_settingsStore.GameDirectory, "data", $"{name}.patch_{i}"));
						installedFiles.Add(dest.FullName);
					}
					if (triplet.GpuResources is not null)
					{
						var dest = triplet.GpuResources.CopyTo(Path.Combine(_settingsStore.GameDirectory, "data", $"{name}.patch_{i}.gpu_resources"));
						installedFiles.Add(dest.FullName);
					}
					if (triplet.Stream is not null)
					{
						var dest = triplet.Stream.CopyTo(Path.Combine(_settingsStore.GameDirectory, "data", $"{name}.patch_{i}.stream"));
						installedFiles.Add(dest.FullName);
					}
				}
			}

			_logger.LogInformation("Saving installed file list");
			await File.WriteAllLinesAsync(Path.Combine(_settingsStore.StorageDirectory, "installed.txt"), installedFiles);

			_logger.LogInformation("Deployment success");
			return true;
		}

		public async Task PurgeAsync()
		{
			_logger.LogInformation("Purging mods");
			var path = Path.Combine(_settingsStore.StorageDirectory, "installed.txt");

			if (File.Exists(path))
			{
				_logger.LogInformation("Reading installed file list");
				var installedFiles = await File.ReadAllLinesAsync(path);

				_logger.LogInformation("Deleting files");
				foreach (var file in installedFiles)
					File.Delete(file);

				_logger.LogInformation("Deleting installed file list");
				File.Delete(path);
			}

			_logger.LogInformation("Purge complete");
		}

		private bool IsGuidFree(Guid guid)
		{
			foreach (var mod in _mods)
				if (mod.Manifest.Guid == guid)
					return false;
			return true;
		}

		private Guid GetFreeGuid()
		{
			Guid guid;
			do
			{
				guid = Guid.NewGuid();
			}
			while (!IsGuidFree(guid));
			return guid;
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
		private static partial Regex GetParchFileRegex();

		[GeneratedRegex(@"\.patch_[0-9]+")]
		private static partial Regex GetParchRegex();
	}
}
