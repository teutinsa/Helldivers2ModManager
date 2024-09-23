using System.IO;

namespace Helldivers2ModManager.Models
{
	internal sealed class ModData
	{
		public DirectoryInfo Directory { get; }

		public ModManifest Manifest { get; }

		public int Option { get; set; }

		public bool Enabled { get; set; }

		public ModData(DirectoryInfo dir, ModManifest manifest)
		{
			Directory = dir;
			Manifest = manifest;
			Option = -1;
			Enabled = true;
		}
	}
}
