using System.IO;

namespace Helldivers2ModManager.Models;

// Positive values are warnings
// Negative values are errors
internal enum ModProblemKind
{
	EmptyImagePath = 6,
	InvalidImagePath = 5,
	EmptyIncludes = 4,
	EmptySubOptions = 3,
	EmptyOptions = 2,
	NoManifestFound = 1,
	CantParseManifest = -1,
	UnknownManifestVersion = -2,
	OutOfSupportManifest = -3,
	Duplicate = -4,
	InvalidPath = -5,
}

internal class ModProblem
{
	public required DirectoryInfo Directory { get; init; }

	public required ModProblemKind Kind { get; init; }

	public object? ExtraData { get; init; }

	public bool IsError => Kind < 0;
}
