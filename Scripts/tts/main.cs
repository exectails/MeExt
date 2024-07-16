//--- Melia Script ----------------------------------------------------------
// TTS
//--- Description -----------------------------------------------------------
// Enables AI-generated voice acting for NPC dialogs.
// Requires the extension application MeExt to be installed on the client.
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Melia.Shared.Scripting;
using Melia.Zone.Events;
using Melia.Zone.Scripting;
using ScriptsZone.Custom.TTS;
using Yggdrasil.Logging;
using Yggdrasil.Scripting;

[Priority(99)]
public class TtsClientScript : ClientScript
{
	private readonly JsonSerializerOptions _jsonOptions = new() { AllowTrailingCommas = true };

	private TtsConf _conf;
	private NpcEntry[] _npcEntries;
	private bool _ready;

	private readonly string[] _maleVoices = ["Adam", "Antoni", "Brian", "Clyde", "Daniel", "Fin", "George", "James"];
	private readonly string[] _femaleVoices = ["Alice", "Charlotte", "Dorothy", "Emily", "Freya", "Glinda", "Lily", "Sarah"];

	public override void Load()
	{
		this.LoadAllScripts();
		this.LoadConf();
		this.LoadNpcEntries();

		_ready = _conf != null && _npcEntries != null;
		if (!_ready)
			Log.Error("TtsClientScript: Failed to load.");
	}

	private void LoadConf()
	{
		var folderPath = Path.GetDirectoryName(this.GetCallingFilePath());
		var filePath = Path.Combine(folderPath, "conf.json");

		if (!File.Exists(filePath))
		{
			Log.Error("TtsClientScript: File 'npcs.json' not found.");
			return;
		}

		try
		{
			var contents = File.ReadAllText(filePath);
			_conf = JsonSerializer.Deserialize<TtsConf>(contents, _jsonOptions);
		}
		catch (Exception ex)
		{
			Log.Error("TtsClientScript: Failed to read conf file. Error: {0}", ex);
		}

		if (_conf.ApiKey == "YOUR_API_KEY")
		{
			Log.Error("TtsClientScript: Please set your API key in 'conf.json'.");
			return;
		}
	}

	private void LoadNpcEntries()
	{
		var folderPath = Path.GetDirectoryName(this.GetCallingFilePath());
		var filePath = Path.Combine(folderPath, "npcs.json");

		if (!File.Exists(filePath))
		{
			Log.Error("TtsClientScript: File 'npcs.json' not found.");
			return;
		}

		try
		{
			var contents = File.ReadAllText(filePath);
			_npcEntries = JsonSerializer.Deserialize<NpcEntry[]>(contents, _jsonOptions);
		}
		catch (Exception ex)
		{
			Log.Error("TtsClientScript: Failed to read NPC data. Error: {0}", ex);
		}
	}

	[On("PlayerReady")]
	protected void OnPlayerReady(object sender, PlayerEventArgs e)
	{
		if (!_ready)
			return;

		this.SendAllScripts(e.Character);
	}

	[On("PlayerDialog")]
	protected void OnPlayerDialog(object sender, PlayerDialogEventArgs e)
	{
		if (!_ready)
			return;

		try
		{
			this.GenerateAudio(e);
		}
		catch (Exception ex)
		{
			Log.Error("TtsClientScript: " + ex);
		}
	}

	private async void GenerateAudio(PlayerDialogEventArgs e)
	{
		var classId = e.Npc.Id;
		var npcName = e.Npc.Name;

		var dialogText = e.DialogText;
		dialogText = dialogText.Replace("\"", "'");
		dialogText = dialogText.Replace("&", "and");
		dialogText = dialogText.Replace("{nl}", " ");
		dialogText = dialogText.Replace("{np}", " ");

		var dialogTitle = e.DialogTitle;

		var voiceName = dialogTitle;
		var text = dialogText;

		var npcEntry = _npcEntries.FirstOrDefault(entry => entry.ClassId == classId);
		if (npcEntry != null)
		{
			voiceName = npcEntry.Voice;
		}

		var hashValue = dialogTitle + text;
		var hash = BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(hashValue))).Replace("-", "").ToLower();

		//Log.Debug("Generate TTS:");
		//Log.Debug("- NPC: {0}", npcName);
		//Log.Debug("- Voice: {0}", voiceName);
		//Log.Debug("- Hash: {0}", hash);
		//Log.Debug("- Dialog: {0}", text);

		var filePath = Path.Combine("user", "web", "generated", "tts", hash + ".mp3");
		var filePathGenerating = filePath + ".generating";

		if (File.Exists(filePath))
		{
			//Log.Debug("File already exists.");
			return;
		}

		if (File.Exists(filePathGenerating))
		{
			//Log.Debug("File is already being generated.");
			return;
		}

		var folderPath = Path.GetDirectoryName(filePath);
		if (!Directory.Exists(folderPath))
			Directory.CreateDirectory(folderPath);

		using (var fs = File.OpenWrite(filePathGenerating))
		{
			var api = new ElevenApi(_conf.ApiKey);
			var voice = await this.GetVoice(api, voiceName);

			//Log.Debug("- Actual Voice: {0}", voice.Name);

			var buffer = await api.Generate(voice, text);
			await fs.WriteAsync(buffer);
		}

		File.Delete(filePath);
		File.Move(filePathGenerating, filePath);

		//Log.Debug("Saved audio file at '{0}'.", filePath);
	}

	private async Task<Voice> GetVoice(ElevenApi api, string voiceName)
	{
		var match = Regex.Match(voiceName, @"^(?<gender>Male|Female|Unknown)(?<id>[0-9]+)$");
		if (match.Success)
		{
			var gender = match.Groups["gender"].Value;
			var id = int.Parse(match.Groups["id"].Value) - 1;

			if (gender == "Female" || gender == "Unknown")
				voiceName = _femaleVoices[id % _femaleVoices.Length];
			else
				voiceName = _maleVoices[id % _maleVoices.Length];
		}

		return await api.GetVoice(voiceName);
	}

	private class NpcEntry
	{
		public int ClassId { get; set; }
		public string ClassName { get; set; }
		public string Name { get; set; }
		public string Gender { get; set; }
		public string Voice { get; set; }
	}

	private class TtsConf
	{
		public string ApiKey { get; set; }
	}
}
