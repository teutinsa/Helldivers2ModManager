using Helldivers2ModManager.Services.Nexus;
using System.Net.Http;
using System.Text.Json;

namespace Helldivers2ModManager.Services;

internal sealed class NexusService : IDisposable
{
	private readonly HttpClient _client;
	private string? _apiKey;

	public NexusService()
	{
		_client = new()
		{
			BaseAddress = new Uri("https://api.nexusmods.com")
		};
		_client.DefaultRequestHeaders.Add("accept", "application/json");
		_client.DefaultRequestHeaders.Add("Application-Version", App.Version.ToString());
		_client.DefaultRequestHeaders.Add("Application-name", "HD2ModManager");
	}

	public void UseApiKey(string key)
	{
		_apiKey = key;
		_client.DefaultRequestHeaders.Remove("apiKey");
		_client.DefaultRequestHeaders.Add("apiKey", _apiKey);
	}

	/// <summary>
	/// Asynchronously gets an array of <see cref="NexusMod"/>s that have been updated in the past week.
	/// </summary>
	/// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns>The Task object representing the asynchronous operation.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no API key is set.</exception>
	public async Task<NexusMod?[]?> GetUpdatedAsync(CancellationToken cancellationToken = default)
	{
		if (_apiKey is null)
			throw new InvalidOperationException("No API key set!");

		var response = await _client.GetAsync($"v1/games/helldivers2/mods/updated.json?period=1w", cancellationToken);

		if (!response.IsSuccessStatusCode)
			return null;

		using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		//var triplets = await JsonSerializer.DeserializeAsync<UpdateTriplet[]>(stream, JsonSerializerOptions.Web, cancellationToken);
		var doc = await JsonDocument.ParseAsync(stream, default, cancellationToken);
		var root = doc.RootElement;
		var triplets = new UpdateTriplet[root.GetArrayLength()];
		var array = root.EnumerateArray();
		for (int i = 0; i < triplets.Length; i++)
		{
			array.MoveNext();
			var elm = array.Current;
			triplets[i] = new UpdateTriplet
			{
				ModId = elm.GetProperty("mod_id").GetInt32(),
				LatestFileUpdate = elm.GetProperty("latest_file_update").GetInt32(),
				LatestModActivity = elm.GetProperty("latest_mod_activity").GetInt32()
			};
		}

		if (triplets is null)
			return null;

		var tasks = new Task<NexusMod?>[triplets.Length];
		for (int i = 0; i < triplets.Length; i++)
		{
			var triplet = triplets[i];
			tasks[i] = GetModAsync(triplet.ModId, cancellationToken);
		}

		return await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Asynchronously gets a <see cref="NexusMod"/> by <paramref name="id"/>.
	/// </summary>
	/// <param name="id">The id of the mod.</param>
	/// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns>The Task object representing the asynchronous operation.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no API key is set.</exception>
	public async Task<NexusMod?> GetModAsync(int id, CancellationToken cancellationToken = default)
	{
		if (_apiKey is null)
			throw new InvalidOperationException("No API key set!");

		var response = await _client.GetAsync($"v1/games/helldivers2/mods/{id}.json");
		
		if (!response.IsSuccessStatusCode)
			return null;

		using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
		return await NexusMod.CreateAsync(body, cancellationToken);
	}

	public void Dispose()
	{
		_client.Dispose();
	}
}
