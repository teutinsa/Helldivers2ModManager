using System;
using System.IO;
using WixSharp;
using WixSharp.Forms;

namespace Installer
{
	public class Program
	{
		static void Main()
		{
			var rootDir = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;

			var project = new ManagedProject
			{
				Name = "Helldivers2ModManager",
				GUID = new Guid("e5cd740f-6f38-4289-bce9-3571af0a8e9a"),
				ProductId = Guid.NewGuid(),
				Version = new Version(1, 0, 1, 0),
				UpgradeCode = new Guid("b07936d3-c8c4-40ea-a323-e31e057d7972"),
				Dirs = new[]
				{
					new Dir(
						@"%ProgramFiles%\Helldivers2ModManager",
						new Files(
							$@"{rootDir}\Helldivers2ModManager\bin\Release\net8.0-windows\*.*",
							f => f.EndsWith(".exe") || f.EndsWith(".dll") || f.EndsWith(".json")
						)
					)
				},
				Platform = Platform.x64,
				ManagedUI = ManagedUI.Empty,
				OutFileName = "HD2MM",
				MajorUpgradeStrategy = MajorUpgradeStrategy.Default
			};

			project.ManagedUI.InstallDialogs.Add(Dialogs.Welcome)
				.Add(Dialogs.InstallDir)
				.Add(Dialogs.Progress)
				.Add(Dialogs.Exit);

			project.BuildMsi();
		}
	}
}