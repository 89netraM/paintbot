﻿namespace PaintBot
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using Game.Configuration;
	using Messaging;
	using Messaging.Request;
	using Messaging.Request.HeartBeat;
	using Messaging.Response;
	using Serilog;
	using Action = Game.Action.Action;

	public abstract class PaintBot
	{
		private readonly IPaintBotClient _paintBotClient;
		private readonly IHearBeatSender _heartBeatSender;
		private readonly ILogger _logger;
		private readonly AnsiPrinter ansiPrinter = new AnsiPrinter();
		private readonly VisualMode visualMode;
		private readonly int _gameLengthInSeconds;
		private readonly Stopwatch stopwatch = new Stopwatch();

		private bool _hasGameEnded;
		private bool _hasTournamentEnded;
		private string _playerId;
		private string gameUrl;

		public GameSettings GameSettings { get; private set; }

		public event System.Action<GameStarting> GameStartingEvent;
		public event System.Action<MapUpdated> MapUpdatedEvent;
		public event System.Action<long> TimingEvent;


		protected PaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender heartBeatSender, ILogger logger)
		{
			_paintBotClient = paintBotClient;
			_heartBeatSender = heartBeatSender;
			_logger = logger;
			visualMode = paintBotConfig.VisualMode;
			_gameLengthInSeconds = paintBotConfig.GameLengthInSeconds;
		}

		public abstract GameMode GameMode { get; }
		public abstract string Name { get; }
		public abstract Action GetAction(MapUpdated mapUpdated);
		public virtual Action? GetOverrideAction() => null;

		public async Task Run(CancellationToken ct)
		{
			try
			{
				await _paintBotClient.ConnectAsync(GameMode, ct);

				var gameSettings = new GameSettings { GameDurationInSeconds = _gameLengthInSeconds };
				await _paintBotClient.SendAsync(new RegisterPlayer(Name, gameSettings), ct);

				await foreach (var response in _paintBotClient.ReceiveEnumerableAsync<Response>(ct))
				{
					await HandleResponseAsync(response, ct);
					if (!IsPlaying())
					{
						break;
					}
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
			}
			finally
			{
				_paintBotClient?.Close();
			}
		}

		private Task HandleResponseAsync(Response response, CancellationToken ct)
		{
			return response switch
			{
				PlayerRegistered playerRegistered => OnPlayerRegistered(playerRegistered, ct),
				MapUpdated mapUpdated => OnMapUpdated(mapUpdated, ct),
				GameLink gameLink => OnGameLink(gameLink),
				GameStarting gameStarting => OnGameStarting(gameStarting),
				GameResult gameResult => OnInfoEvent(gameResult),
				CharacterStunned characterStunned => OnInfoEvent(characterStunned),
				HeartBeatResponse heartBeatResponse => OnHearBeatEvent(heartBeatResponse, ct),
				GameEnded gameEnded => OnGameEnded(gameEnded),
				TournamentEnded tournamentEnded => OnTournamentEnded(tournamentEnded),
				InvalidPlayerName invalidPlayerName => OnInfoEvent(invalidPlayerName),
				_ => Task.CompletedTask
			};
		}

		private Task OnTournamentEnded(TournamentEnded tournamentEnded)
		{
			_hasTournamentEnded = true;
			_logger.Information("The tournament has ended"); // Don't spoil the results in the console. 
			return Task.CompletedTask;
		}

		private async Task OnPlayerRegistered(PlayerRegistered playerRegistered, CancellationToken ct)
		{
			_playerId = playerRegistered.ReceivingPlayerId;
			SendHearBeat(ct);
			await _paintBotClient.SendAsync(new StartGame(playerRegistered.ReceivingPlayerId), ct);
			await _paintBotClient.SendAsync(new ClientInfo("C#", "9", Environment.OSVersion?.Platform.ToString(), Environment.OSVersion?.VersionString, "1", playerRegistered.ReceivingPlayerId), ct);
			_logger.Information(playerRegistered.ToString());
		}

		private async Task OnMapUpdated(MapUpdated mapUpdated, CancellationToken ct)
		{
			if (visualMode == VisualMode.GUI)
			{
				stopwatch.Restart();
			}
			if (visualMode == VisualMode.Ansi)
			{
				ansiPrinter.SetupPlayers(mapUpdated.ReceivingPlayerId, mapUpdated.Map.CharacterInfos);
				System.Console.Write($"\x1b[s\x1b[0;0H{ansiPrinter.WriteMap(mapUpdated.Map)}\x1b[0m\x1b[u");
			}
			else
			{
				_logger.Information($"{mapUpdated}");
			}
			MapUpdatedEvent?.Invoke(mapUpdated);
			var action = GetAction(mapUpdated);
			if (visualMode == VisualMode.GUI)
			{
				stopwatch.Stop();
				int additionalWaitTime = (int)Math.Round(GameSettings.TimeInMsPerTick * 0.55f - stopwatch.ElapsedMilliseconds);
				TimingEvent?.Invoke(stopwatch.ElapsedMilliseconds);
				if (additionalWaitTime > 0)
				{
					await Task.Delay(additionalWaitTime, ct);
				}
			}
			action = GetOverrideAction() ?? action;
			await _paintBotClient.SendAsync(
				new RegisterMove(mapUpdated.ReceivingPlayerId)
				{
					GameId = mapUpdated.GameId,
					GameTick = mapUpdated.GameTick,
					Direction = action.ToString().ToUpper()
				}, ct);
		}

		private Task OnGameStarting(GameStarting gameStarting)
		{
			GameSettings = gameStarting.GameSettings;
			GameStartingEvent?.Invoke(gameStarting);
			return OnInfoEvent(gameStarting);
		}

		private Task OnGameLink(GameLink gameLink)
		{
			gameUrl = gameLink.Url;
			return OnInfoEvent(gameLink);
		}

		private Task OnInfoEvent(Response response)
		{
			if (response is GameResult)
			{
				if (GameMode == GameMode.Training)
				{
					_logger.Information($"{response}");
				}
			}
			else
			{
				_logger.Information($"{response}");
			}

			return Task.CompletedTask;
		}

		private Task OnHearBeatEvent(HeartBeatResponse heartBeat, CancellationToken ct)
		{
			_logger.Information(heartBeat.ToString());
			SendHearBeat(ct);
			return Task.CompletedTask;
		}

		protected virtual Task OnGameEnded(GameEnded response)
		{
			_hasGameEnded = true;
			if (GameMode == GameMode.Training)
			{
				_logger.Information(response.ToString());
			}
			else if (GameMode == GameMode.Tournament)
			{
				_logger.Information("The game has ended"); // Don't spoil the result in the console.
			}
			return Task.CompletedTask;
		}

		private bool IsPlaying()
		{
			if (GameMode == GameMode.Training)
			{
				return !_hasGameEnded;
			}

			return !_hasTournamentEnded;
		}

		private void SendHearBeat(CancellationToken ct)
		{
			_heartBeatSender.SendHeartBeatFrom(_playerId, ct);
		}
	}
}
