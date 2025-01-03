using System.IO;

namespace Helldivers2ModManager.Exceptions;

internal sealed class AddFilesException : Exception
{
	public AddFilesException(DirectoryNotFoundException exception)
		: base(exception.Message, exception)
	{ }
}