using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helldivers2ModManager.Services
{
	internal sealed class GitHubService : IDisposable
	{
		public readonly struct Issue
		{
			public sealed class IssueJsonConverter : JsonConverter<Issue>
			{
				public override Issue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				{
					var doc = JsonDocument.ParseValue(ref reader);
					var root = doc.RootElement;

					IssueState state;
					string title;
					string body;

					if (!root.TryGetProperty("state", out var prop))
						throw new JsonException();
					if (!Enum.TryParse(prop.GetString(), true, out state))
						throw new JsonException();
					if (!root.TryGetProperty("title", out prop))
						throw new JsonException();
					title = prop.GetString() ?? throw new JsonException();
					if (!root.TryGetProperty("body", out prop))
						throw new JsonException();
					body = prop.GetString() ?? throw new JsonException();

					return new()
					{
						State = state,
						Title = title,
						Body = body
					};
				}

				public override void Write(Utf8JsonWriter writer, Issue value, JsonSerializerOptions options)
				{
					throw new NotImplementedException();
				}
			}

			public enum IssueState
			{
				Open,
				Closed
			}

			public required IssueState State { get; init; }

			public required string Title { get; init; }

			public required string Body { get; init; }
		}

		private static readonly string s_patToken = "";
		private static readonly JsonSerializerOptions s_options;
		private readonly HttpClient _client;

		static GitHubService()
		{
			s_options = new();
			s_options.Converters.Add(new Issue.IssueJsonConverter());
		}

		public GitHubService()
		{
			_client = new()
			{
				BaseAddress = new Uri("https://api.github.com")
			};
			_client.DefaultRequestHeaders.Add("Accept", "application/json");
			_client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
			_client.DefaultRequestHeaders.Add("User-Agent", "Helldivers2ModManager");
			_client.DefaultRequestHeaders.Add("Authorization", $"token {s_patToken}");
		}

		public async Task<Issue[]> GetIssuesAsync()
		{
			var result = await _client.GetAsync("repos/teutinsa/Helldivers2ModManager/issues");

			using var stream = result.Content.ReadAsStream();
			var issues = await JsonSerializer.DeserializeAsync<Issue[]>(stream, s_options) ?? throw new JsonException();
			
			return issues;
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}
