using System.Security.Cryptography;
using System.Text;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MeExt.TTS.ElevenLabs
{
	internal class ElevenTtsMessage : ITtsMessage
	{
		private readonly ElevenApi _api;
		private readonly string _voice;
		private readonly string _text;

		private StreamMediaFoundationReader _audio;
		private WasapiOut _output;

		public bool Done { get; private set; }

		public ElevenTtsMessage(string apiKey, string voice, string text)
		{
			_api = new ElevenApi(apiKey);
			_voice = voice;
			_text = text;
		}

		~ElevenTtsMessage()
		{
			this.Dispose();
		}

		private void Dispose()
		{
			try { _output?.Dispose(); } catch { }
			try { _audio?.Dispose(); } catch { }
		}

		private async Task<Voice> GetVoice(string voiceIdent)
		{
			var voices = await _api.GetVoices();
			var selectedVoice = voices.Voices.FirstOrDefault(v => v.Id == voiceIdent || v.Name.Contains(voiceIdent, StringComparison.InvariantCultureIgnoreCase));

			if (selectedVoice == null)
			{
				// Choose a female voice other than Rachel by default, as Rachel
				// seems to be a bit quiet.
				selectedVoice = voices.Voices.FirstOrDefault(v => v.Name != "Rachel" && v.Labels.Gender == "female");

				if (selectedVoice == null)
					selectedVoice = voices.Voices.First();
			}

			return selectedVoice;
		}

		public async Task Start()
		{
			var hashValue = _voice + _text;
			var hash = BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(hashValue))).Replace("-", "").ToLower();

			var cacheFolder = Path.Combine("MmExt", "ElevenLabs", "Cache");
			var filePath = Path.Combine(cacheFolder, hash + ".mp3");

			if (!Directory.Exists(cacheFolder))
				Directory.CreateDirectory(cacheFolder);

			if (File.Exists(filePath))
			{
				using var fs = File.OpenRead(filePath);
				await this.PlayStream(fs);
				return;
			}

			var voice = await this.GetVoice(_voice);

			var buffer = await _api.Generate(voice, _text);
			File.WriteAllBytes(filePath, buffer);

			using var ms = new MemoryStream(buffer);
			await this.PlayStream(ms);
		}

		public void Stop()
		{
			_output?.Stop();

			this.Done = true;
			this.Dispose();
		}

		private async Task PlayStream(Stream stream)
		{
			_audio = new StreamMediaFoundationReader(stream);
			_output = new WasapiOut(AudioClientShareMode.Shared, 100);
			_output.Init(_audio);
			_output.Play();

			while (_output.PlaybackState == PlaybackState.Playing)
				await Task.Delay(250);

			this.Stop();
		}
	}
}
