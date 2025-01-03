using Helldivers2ModManager.Models;
using Helldivers2ModManager.Stores;
using System.IO;
using System.Security;

namespace Helldivers2ModManager.Exceptions;

internal sealed class DeployException : Exception
{
	public ModData? Mod { get; }

	public ModStore.PatchFileTriplet? FileTriplet { get; }

	public DeployException(PurgeException exception)
		: base(exception.Message, exception)
	{ }

	public DeployException(IOException exception)
		: base("Temp directory can not be created or deleted because it's read-only!", exception)
	{ }

	public DeployException(UnauthorizedAccessException exception)
		: base("Temp directory contains a read-only file!", exception)
	{ }

	public DeployException(DirectoryNotFoundException exception)
		: base("Temp directory does not exist or can not be found!", exception)
	{ }

	public DeployException(SecurityException exception)
		: base("The manager does not have the required permission!", exception)
	{ }


	public DeployException(ModData mod, AddFilesException exception)
		: base("Error adding files of mod to groups!", exception)
	{
		Mod = mod;
	}

	public DeployException(ModData mod, IndexOutOfRangeException exception)
		: base("Option index out of range!", exception)
	{
		Mod = mod;
	}

	public DeployException(ModStore.PatchFileTriplet triplet, SecurityException exception)
		: base("The manager does not have the required permission!", exception)
	{
		FileTriplet = triplet;
	}

	public DeployException(ModStore.PatchFileTriplet triplet, UnauthorizedAccessException exception)
		: base("Patch file is either readonly, hidden, or the manager does not have the required permission!", exception)
	{
		FileTriplet = triplet;
	}

	public DeployException(ModStore.PatchFileTriplet triplet, PathTooLongException exception)
		: base(exception.Message, exception)
	{
		FileTriplet = triplet;
	}

	public DeployException(ModStore.PatchFileTriplet triplet, DirectoryNotFoundException exception)
		: base(exception.Message, exception)
	{
		FileTriplet = triplet;
	}

	public DeployException(ModStore.PatchFileTriplet triplet, IOException exception)
		: base("An I/O error occurred while creating the file or the destination file already exists!", exception)
	{
		FileTriplet = triplet;
	}

	public DeployException(ModStore.PatchFileTriplet triplet, NotSupportedException exception)
		: base("The path is in an invalid format!", exception)
	{
		FileTriplet = triplet;
	}
}
