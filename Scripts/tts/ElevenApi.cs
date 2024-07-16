using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptsZone.Custom.TTS
{
	internal class ElevenApi
	{
		private readonly string _apiKey;

		public ElevenApi(string apiKey)
		{
			_apiKey = apiKey;
		}

		public async Task<VoicesResult> GetVoices()
		{
			var http = new HttpClient();

			var req = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
			req.Headers.Add("Accept", "application/json");
			req.Headers.Add("xi-api-key", _apiKey);

			var result = await http.SendAsync(req);
			if (!result.IsSuccessStatusCode)
				return null;

			var content = await result.Content.ReadAsStringAsync();
			var voices = JsonSerializer.Deserialize<VoicesResult>(content);

			return voices;
		}

		public async Task<Voice> GetVoice(string voiceIdent)
		{
			var voices = await this.GetVoices();
			var selectedVoice = voices.Voices.FirstOrDefault(v => v.Id == voiceIdent || v.Name.Contains(voiceIdent, StringComparison.InvariantCultureIgnoreCase));

			if (selectedVoice == null)
			{
				// Choose a female voice other than Rachel by default,
				// as Rachel seems to be a bit quiet.
				selectedVoice = voices.Voices.FirstOrDefault(v => v.Name != "Rachel" && v.Labels.Gender == "female");

				if (selectedVoice == null)
					selectedVoice = voices.Voices.First();
			}

			return selectedVoice;
		}

		public async Task<byte[]> Generate(Voice voice, string text)
		{
			var http = new HttpClient();

			var req = new HttpRequestMessage(HttpMethod.Post, $"https://api.elevenlabs.io/v1/text-to-speech/{voice.Id}/stream");
			req.Headers.Add("Accept", "application/json");
			req.Headers.Add("xi-api-key", _apiKey);

			var data = new
			{
				text,
				model_id = "eleven_multilingual_v2",
				//model_id = "eleven_turbo_v2",
				voice_settings = new
				{
					stability = 0.5,
					similarity_boost = 0.8,
					style = 0.0,
					use_speaker_boost = false,
				}
			};

			req.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

			var result = await http.SendAsync(req);
			if (!result.IsSuccessStatusCode)
				return [];

			using var stream = await result.Content.ReadAsStreamAsync();
			using var ms = new MemoryStream();

			stream.CopyTo(ms);
			return ms.ToArray();
		}
	}

	internal class Voice
	{
		[JsonPropertyName("voice_id")]
		public string Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("labels")]
		public Labels Labels { get; set; }
	}

	public class Labels
	{
		[JsonPropertyName("accent")]
		public string Accent { get; set; }

		[JsonPropertyName("description")]
		public string Description { get; set; }

		[JsonPropertyName("use case")]
		public string UseCase { get; set; }

		[JsonPropertyName("gender")]
		public string Gender { get; set; }

		[JsonPropertyName("age")]
		public string Age { get; set; }
	}

	internal class VoicesResult
	{
		[JsonPropertyName("voices")]
		public Voice[] Voices { get; set; }
	}
}
