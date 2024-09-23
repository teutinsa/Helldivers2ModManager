using System;
using WixSharp;

namespace Installer
{
	public class Program
	{
		static void Main()
		{
			var project = new ManagedProject()
			{
				Name = "Helldivers2ModManager",
				GUID = new Guid("e5cd740f-6f38-4289-bce9-3571af0a8e9a"),
				Dirs = new[]
				{
					new Dir(@"%ProgramFiles%\Helldivers2ModManager")
					{

					}
				}
			};

			project.GUID = new Guid("bc69846a-df9f-4019-af12-dc6b25587137");
			project.ManagedUI = ManagedUI.Default;
			project.BuildMsi();
		}
	}
}