using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Helldivers2ModManager.Stores;

internal sealed class SSOStore
{
	private sealed class SSOResponse
	{
		public required bool Success { get; set; }

		public required Dictionary<string, string> Data { get; set; }

		public string? Error { get; set; }
	}

	private const string WebSocketUrl = "wss://sso.nexusmods.com";
	private const string ApplicationSlug = "hd2mm";
	private const string NexusModsSSOUrl = "https://www.nexusmods.com/sso";

	private Guid? _uuid;
	private string? _connetionToken;
	private string? _apiKey;

	public async Task<string?> GetApiKeyAsync()
	{
		if (_apiKey is not null)
			return _apiKey;

		var uuid = GetUuid();
		using var socket = new ClientWebSocket();

		await socket.ConnectAsync(new Uri(WebSocketUrl), CancellationToken.None);

		var request = new
		{
			id = uuid,
			token = _connetionToken,
			protocol = 2
		};

		var requestJson = JsonSerializer.Serialize(request);
		await socket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, CancellationToken.None);

		Process.Start(new ProcessStartInfo
		{
			FileName = $"{NexusModsSSOUrl}?id={uuid}&application={ApplicationSlug}",
			UseShellExecute = true
		});

		var buffer = new byte[1024];
		while (socket.State == WebSocketState.Open)
		{
			var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
			if (result.MessageType == WebSocketMessageType.Close)
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
			else
			{
				var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
				Console.WriteLine(responseJson);
				var response = JsonSerializer.Deserialize<SSOResponse>(responseJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
				if (response is SSOResponse { Success: true })
				{
					if (response.Data.TryGetValue("connection_token", out var token))
						_connetionToken = token;
					else if (response.Data.TryGetValue("api_key", out var key))
					{
						_apiKey = key;
						break;
					}
				}
			}
		}

		return _apiKey;
	}

	private Guid GetUuid()
	{
		if (!_uuid.HasValue)
			_uuid = Guid.NewGuid();
		return _uuid.Value;
	}
}
