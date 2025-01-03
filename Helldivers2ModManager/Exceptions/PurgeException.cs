using System.IO;
using System.Security;

namespace Helldivers2ModManager.Exceptions;

internal sealed class PurgeException : Exception
{
    public FileInfo? File { get; }

    public PurgeException(FileInfo file, IOException exception)
        : base(null, exception)
    {
        File = file;
    }

    public PurgeException(FileInfo file, SecurityException exception)
        : base(null, exception)
    {
        File = file;
    }

    public PurgeException(FileInfo file, UnauthorizedAccessException exception)
        : base(null, exception)
    {
        File = file;
    }

    public PurgeException(DirectoryNotFoundException exception)
        : base(null, exception)
    { }

    public PurgeException(SecurityException exception)
        : base(null, exception)
    { }
}
