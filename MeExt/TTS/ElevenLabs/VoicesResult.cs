using System.Text.Json.Serialization;

namespace MeExt.TTS.ElevenLabs
{
	internal class VoicesResult
	{
		[JsonPropertyName("voices")]
		public Voice[] Voices { get; set; }
	}
}
