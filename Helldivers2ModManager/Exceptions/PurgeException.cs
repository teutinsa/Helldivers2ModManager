using System.IO;
using System.Security;

namespace Helldivers2ModManager.Exceptions;

internal sealed class PurgeException : Exception
{
    public FileInfo? File { get; }

    public PurgeException(FileInfo file, IOException exception)
        : base($"The file \"{file.FullName}\" is still open!", exception)
    {
        File = file;
    }

    public PurgeException(FileInfo file, SecurityException exception)
        : base($"The manager does not have the required permission to delete the file \"{file.FullName}\"!", exception)
    {
        File = file;
    }

    public PurgeException(FileInfo file, UnauthorizedAccessException exception)
        : base("The path is a directory!", exception)
    {
        File = file;
    }

    public PurgeException(DirectoryNotFoundException exception)
        : base("The path is invalid!", exception)
    { }

    public PurgeException(SecurityException exception)
        : base("The manager does not have the required permission!", exception)
    { }
}
