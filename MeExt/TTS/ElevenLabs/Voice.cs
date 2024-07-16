using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MeExt.TTS.ElevenLabs
{
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
}
