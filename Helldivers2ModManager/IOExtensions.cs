using System.IO;

namespace Helldivers2ModManager;

internal static class IOExtensions
{
	public static void CopyTo(this DirectoryInfo info, string destDirName)
	{
		Directory.CreateDirectory(destDirName);

		foreach (var file in info.EnumerateFiles("*", SearchOption.AllDirectories))
		{
			var targetFileName = file.FullName.Replace(info.FullName, destDirName);
			var targetDirName = Path.GetDirectoryName(targetFileName)!;

			if (!Directory.Exists(targetDirName))
				Directory.CreateDirectory(targetDirName);

			File.Copy(file.FullName, targetFileName);
		}
	}
}
