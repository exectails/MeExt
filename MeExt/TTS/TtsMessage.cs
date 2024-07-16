namespace MeExt.TTS
{
	internal interface ITtsMessage
	{
		bool Done { get; }

		Task Start();
		void Stop();
	}
}
