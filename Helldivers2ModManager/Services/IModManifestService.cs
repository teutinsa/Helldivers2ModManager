using System.IO;
using System.Text.Json;

namespace Helldivers2ModManager.Services;

internal interface IModManifestService
{
	protected static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp"];
	protected static readonly JsonDocumentOptions DocOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip,
		MaxDepth = 64
	};

	Task<object?> FromFileAsync(FileInfo file, CancellationToken cancellationToken = default);

	Task<object?> InferrFromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default);

	Task<object?> FromDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken = default);

	Task ToFileAsync(object manifest, FileInfo dest, CancellationToken cancellationToken = default);
}
