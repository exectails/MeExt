using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MeExt.TTS.Melia
{
	internal class MeliaTtsMessage : ITtsMessage
	{
		private readonly string _voice;
		private readonly string _text;

		private StreamMediaFoundationReader _audio;
		private WasapiOut _output;

		public bool Done { get; private set; }

		public MeliaTtsMessage(string voice, string text)
		{
			_voice = voice;
			_text = text;
		}

		~MeliaTtsMessage()
		{
			this.Dispose();
		}

		private void Dispose()
		{
			try { _output?.Dispose(); } catch { }
			try { _audio?.Dispose(); } catch { }
		}

		public async Task Start()
		{
			var hashValue = _voice + _text;
			var hash = BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(hashValue))).Replace("-", "").ToLower();

			using var stream = await this.GetStream(hash);
			if (stream == null)
			{
				this.Stop();
				return;
			}

			await this.PlayStream(stream);
		}

		public void Stop()
		{
			try { _output?.Stop(); } catch { }

			this.Done = true;
			this.Dispose();
		}

		private async Task<Stream> GetStream(string hash)
		{
			var cacheFolder = Path.Combine("MmExt", "TTS", "Cache");
			var filePath = Path.Combine(cacheFolder, hash + ".mp3");

			if (!Directory.Exists(cacheFolder))
				Directory.CreateDirectory(cacheFolder);

			if (File.Exists(filePath))
			{
				var fs = File.OpenRead(filePath);
				return fs;
			}

			var buffer = await this.Download(hash);
			if (buffer == null)
				return null;

			await File.WriteAllBytesAsync(filePath, buffer);

			return new MemoryStream(buffer);
		}

		private async Task<byte[]> Download(string hash)
		{
			var http = new HttpClient();

			var url = "http://127.0.0.1:8080/generated/tts/" + hash + ".mp3";
			var result = await http.GetAsync(url);

			if (!result.IsSuccessStatusCode)
			{
				var genUrl = url + ".generating";
				var generated = false;

				for (var i = 0; i < 3; ++i)
				{
					var success = await this.WaitWhileGenerating(genUrl);
					if (success)
					{
						generated = true;
						break;
					}

					await Task.Delay(1000);
				}

				if (!generated)
				{
					var req = new HttpRequestMessage(HttpMethod.Head, url);
					result = await http.SendAsync(req);

					if (!result.IsSuccessStatusCode)
						return null;
				}
			}

			result = await http.GetAsync(url);
			if (!result.IsSuccessStatusCode)
				return null;

			using var ms = new MemoryStream();
			await result.Content.CopyToAsync(ms);

			return ms.ToArray();
		}

		private async Task<bool> WaitWhileGenerating(string url)
		{
			var http = new HttpClient();
			var timer = Stopwatch.StartNew();
			var wasOk = false;

			while (true)
			{
				var req = new HttpRequestMessage(HttpMethod.Head, url);
				var result = await http.SendAsync(req);

				if (!result.IsSuccessStatusCode)
					return wasOk;

				wasOk = true;

				if (timer.Elapsed.TotalSeconds >= 20)
					return false;

				await Task.Delay(250);
			}
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
