using System.IO.Pipes;
using MeExt.TTS;
using MeExt.TTS.Melia;
using MeExt.TTS.Microsoft;
using NAudio.Wave;

namespace MeExt
{
	internal static class Program
	{
		private const string MutexName = "MeExtMutex";
		private const string PipeName = "MeExtPipe";

		private static ITtsMessage CurMsg;

		[STAThread]
		private static void Main(string[] args)
		{
			Directory.SetCurrentDirectory(Path.GetDirectoryName(AppContext.BaseDirectory));

			using var mutex = new Mutex(true, MutexName, out var isCreatedNew);

			if (!isCreatedNew)
			{
				Send(args);
				return;
			}

			//if (args.Length == 0)
			//	args = ["speak", "_system", "Let's get this party started!"];

			Handle(args);
			BeginListen();

			while (CurMsg?.Done == false)
				Thread.Sleep(1000);
		}

		private static void Send(string[] args)
		{
			using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
			client.Connect();

			using var bw = new BinaryWriter(client);
			bw.Write(args.Length);
			foreach (var arg in args)
				bw.Write(arg);
			bw.Flush();
		}

		private static void BeginListen()
		{
			var server = new NamedPipeServerStream(PipeName, PipeDirection.In);

			server.BeginWaitForConnection((ar) =>
			{
				if (ar.AsyncState is NamedPipeServerStream pipeServer)
				{
					pipeServer.EndWaitForConnection(ar);

					using var br = new BinaryReader(pipeServer);
					var argCount = br.ReadInt32();
					var args = new string[argCount];
					for (var i = 0; i < argCount; i++)
						args[i] = br.ReadString();

					Handle(args);
				}

				BeginListen();
			},
			server);
		}

		private static void Handle(string[] args)
		{
			if (args.Length == 0)
				return;

			switch (args[0])
			{
				case "speak":
				{
					if (args.Length <= 2)
						break;

					var voice = args[1];
					var text = string.Join(" ", args.Skip(2));

					Speak(voice, text);
					break;
				}
				case "stop":
				{
					Stop();
					break;
				}
				case "exit":
				{
					Exit();
					break;
				}
			}
		}

		private static void Speak(string voice, string text)
		{
			CurMsg?.Stop();

			if (voice == "_system")
			{
				CurMsg = new MsTtsMessage(voice, text);
			}
			else
			{
				CurMsg = new MeliaTtsMessage(voice, text);
			}

			_ = CurMsg.Start();
		}

		private static void Stop()
		{
			CurMsg?.Stop();
		}

		private static void Exit()
		{
			Environment.Exit(0);
		}
	}
}
