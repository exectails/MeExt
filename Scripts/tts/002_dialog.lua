Melia.Hook("DIALOG_ON_INIT", function(original, result, addon, frame)

	addon:RegisterMsg('DIALOG_CLOSE', 'M_TTS_ON_DIALOG_CLOSE')

	return result

end)

Melia.Hook("DIALOG_SHOW_DIALOG_TEXT", function(original, result, frame, text, titleName, voiceName)

	Melia.Log.Info("Voice: {0}, Text: {1}", titleName, text)
	Melia.Tts.Speak(titleName, text)

	return result

end)

function M_TTS_ON_DIALOG_CLOSE(frame, msg, argStr, argNum)
	Melia.Log.Info("M_TTS_ON_DIALOG_CLOSE")
	Melia.Tts.Stop()
end
