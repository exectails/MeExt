MeExt
=============================================================================

MeExt is an extension tool intended to add new functionality
to [Melia][1] servers that could not otherwise be accomplished,
such as auto-generated TTS NPC dialogs.

To install MeExt, simply compile it, ideally to a single executable,
and move the MeExt.exe to your client folder. That's it. Afterwards
you can use custom scripts that make use of it, such as the TTS
script found in this repository.

Disclaimer: This is currently very much a prototype and may well
contain bugs or code that is somewhat janky. You've been warned.

Available scripts
-----------------------------------------------------------------------------

### TTS

The TTS script auto-generates audio files for NPC dialogues via the
ElevenLabs API as they're sent to players. The results are cached
and will then be played via MeExt to enable voice-acting.

To use the script, move it to your zone scripts folder and load
all scripts in the folder from the scripts list.

Example
```text
// scripts_custom.txt
custom/tts/**/*
```

You also need to modify conf.json to add your ElevenLabs API key.
The voices the script uses by default are all standard ones, selected
based on the information in npcs.json. To assign a specific voice to
an NPC type, modify the respective `Voice` property in that file.

For example, if you wanted NPC `20017` (Village Woman) to use the
voice `Serena` from your ElevenLabs voices library, you would modify
the data as such:

Old:
```json
{ "ClassId": 20017, "ClassName": "matron_f", "Name": "Village Woman", "Gender": "Female", "Voice": "Female3" },
```

New:
```json
{ "ClassId": 20017, "ClassName": "matron_f", "Name": "Village Woman", "Gender": "Female", "Voice": "Serena" },
```

Note that the genders were auto-generated and might not be correct.
Pull requests to fix incorrect classifications are welcome.

Additional Info
-----------------------------------------------------------------------------

MeExt loads the TTS audio files from the Melia web server, assumed to
be running on port 8080 and on the same machine as the zone servers.
If you're running the web server on a different port, modify the URL
in `MeExt/TTS/Melia/MeliaTtsMessage.cs`.


[1]: https://github.com/NoCode-NoLife/melia
