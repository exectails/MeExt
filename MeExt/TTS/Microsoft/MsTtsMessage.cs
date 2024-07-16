using System.Globalization;
using System.Speech.Synthesis;

namespace MeExt.TTS.Microsoft
{
	internal class MsTtsMessage : ITtsMessage
	{
		private readonly string _text;
		private readonly SpeechSynthesizer _synthesizer;

		public bool Done { get; private set; }

		public MsTtsMessage(string voice, string text)
		{
			_text = text;

			_synthesizer = new SpeechSynthesizer();
			_synthesizer.SetOutputToDefaultAudioDevice();
			_synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 0, new CultureInfo("en-US"));
		}

		public async Task Start()
		{
			_synthesizer.Speak(_text);

			while (_synthesizer.State == SynthesizerState.Speaking)
				await Task.Delay(250);

			this.Stop();
		}

		public void Stop()
		{
			_synthesizer.Pause();
			this.Done = true;
		}
	}
}
