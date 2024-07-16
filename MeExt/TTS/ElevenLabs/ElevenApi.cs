using System.Text;
using System.Text.Json;

namespace MeExt.TTS.ElevenLabs
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
}
