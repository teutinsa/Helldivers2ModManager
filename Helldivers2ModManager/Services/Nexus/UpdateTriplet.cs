namespace Helldivers2ModManager.Services.Nexus;

internal readonly struct UpdateTriplet
{
	public int ModId { get; init; }

	public int LatestFileUpdate { get; init; }

	public int LatestModActivity { get; init; }
}
