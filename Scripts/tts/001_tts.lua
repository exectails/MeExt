Melia.Tts = {}

local function escapeText(text)
	local escapedText = text

	-- Escaping quotes and ampersands *should* prevent shell injections. I think.
	-- Though it's very possible there's some clever way to get around it still.
	escapedText = escapedText:gsub('"', '\'')
	escapedText = escapedText:gsub('&', 'and')
	escapedText = escapedText:gsub('{nl}', ' ')
	escapedText = escapedText:gsub('{np}', ' ')

	return escapedText
end

Melia.Tts.Speak = function(voice, text)
	local escapedVoice = escapeText(voice)
	local escapedText = escapeText(text)

	local call = 'start ../release/MeExt.exe speak "'..escapedVoice..'" "'..escapedText..'"'
	Melia.Log.Info('Calling: ' .. call)

	os.execute(call)
end

Melia.Tts.Stop = function()
	local call = 'start ../release/MeExt.exe stop'
	Melia.Log.Info('Calling: ' .. call)

	os.execute(call)
end
