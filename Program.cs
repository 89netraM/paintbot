namespace PaintBot
{
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Game.Configuration;
	using Messaging;
	using Messaging.Request.HeartBeat;
	using Microsoft.Extensions.DependencyInjection;
	using Serilog;

	public class Program
	{
		public static Task Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.CursorVisible = false;
			var config = GetConfig(args);
			var services = ConfigureServices();
			var serviceProvider = services.BuildServiceProvider();
			var myBot = new MyPaintBot(config, serviceProvider.GetService<IPaintBotClient>(),
				serviceProvider.GetService<IHearBeatSender>(), serviceProvider.GetService<ILogger>());
			return myBot.Run(CancellationToken.None).ContinueWith(_ => Console.CursorVisible = true);
		}

		private static void ConfigureLogger(IServiceCollection services)
		{
			var logger = new LoggerConfiguration()
				.WriteTo.Console()
				.CreateLogger();

			services.AddSingleton<ILogger>(logger);
		}

		private static IServiceCollection ConfigureServices()
		{
			IServiceCollection services = new ServiceCollection();
			ConfigureLogger(services);
			services.AddTransient<IHearBeatSender, HeartBeatSender>();
			services.AddSingleton<IPaintBotClient, PaintBotClient>();
			services.AddSingleton(new PaintBotServerConfig { BaseUrl = "wss://server.paintbot.cygni.se" });
			return services;
		}

		private static PaintBotConfig GetConfig(string[] args)
		{
			var name = args.ElementAtOrDefault(0) ?? throw new Exception("A bot name must be provided");
			var unparsedGameMode = args.ElementAtOrDefault(1);
			var unparsedGameLengthInSeconds = args.ElementAtOrDefault(2);
			var unparsedShouldWriteMap = args.ElementAtOrDefault(3);

			var couldParseGameMode = Enum.TryParse<GameMode>(unparsedGameMode, out var gameMode);
			var couldParseGameLength = int.TryParse(unparsedGameLengthInSeconds, out var gameLengthInSeconds);
			var couldParseShouldWriteMap = bool.TryParse(unparsedShouldWriteMap, out bool shouldWriteMap);

			if (!couldParseGameMode)
			{
				gameMode = GameMode.Training;
			}
			if (!couldParseGameLength)
			{
				gameLengthInSeconds = 180;
			}

			return new PaintBotConfig(name, gameMode, gameLengthInSeconds, shouldWriteMap);
		}
	}
}
