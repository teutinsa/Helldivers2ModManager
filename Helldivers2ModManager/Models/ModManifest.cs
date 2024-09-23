using System.IO;
using System.Text.Json;

namespace Helldivers2ModManager.Models
{
	internal sealed class ModManifest
	{
		public required Guid Guid { get; init; }

		public required string Name { get; init; }

		public required string Description { get; init; }

		public string? IconPath { get; init; }

		public IReadOnlyList<string>? Options { get; init; }

		private static readonly JsonSerializerOptions s_options;

		static ModManifest()
		{
			s_options = new()
			{
				WriteIndented = true,
				AllowTrailingCommas = true,
				ReadCommentHandling = JsonCommentHandling.Skip,
			};
			s_options.Converters.Add(new ModManifestJsonConverter());
		}

		public static ModManifest? Deserialize(FileInfo file)
		{
			using var stream = file.OpenRead();
			return JsonSerializer.Deserialize<ModManifest>(stream, s_options);
		}

		public void Serialize(FileInfo file)
		{
			using var stream = file.OpenWrite();
			JsonSerializer.Serialize(stream, this, s_options);
		}
	}
}
