namespace PaintBot
{
	using System.Collections.Generic;
	using System.Linq;
	using Game.Action;
	using Game.Configuration;
	using Game.Map;
	using Messaging;
	using Messaging.Request.HeartBeat;
	using Messaging.Response;
	using Serilog;
	using Exception = System.Exception;

	public class MyPaintBot : StatePaintBot
	{
		private static readonly IReadOnlyDictionary<Action, Action[]> sideways = new Dictionary<Action, Action[]>
		{
			{ Action.Left, new[] { Action.Up, Action.Down } },
			{ Action.Right, new[] { Action.Down, Action.Up } },
			{ Action.Up, new[] { Action.Right, Action.Left } },
			{ Action.Down, new[] { Action.Left, Action.Right } },
		};

		public MyPaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender hearBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, hearBeatSender, logger)
		{
			GameMode = paintBotConfig.GameMode;
			Name = paintBotConfig.Name ?? "My c# bot";
		}

		public override GameMode GameMode { get; }

		public override string Name { get; }

		protected override IEnumerable<Action> GetActionSequence()
		{
			// Find and go to closest edge
			Action direction = DirectionOfClosestEdge();
			while (DistanceToEdge(direction) > 0)
			{
				if (MapUtils.CanPlayerPerformAction(PlayerId, direction))
				{
					yield return direction;
				}
				else
				{
					yield return sideways[direction].FirstOrDefault(a => MapUtils.CanPlayerPerformAction(PlayerId, a));
				}
			}
			// Go Around
			while (true)
			{
				direction = NextDirection(direction);
				while (DistanceToEdge(direction) > 0)
				{
					if (MapUtils.CanPlayerPerformAction(PlayerId, direction))
					{
						yield return direction;
					}
					else
					{
						yield return sideways[direction][0];
					}
				}
			}
		}

		private Action DirectionOfClosestEdge()
		{
			MapCoordinate player = MapUtils.GetCoordinateOf(PlayerId);
			return new (int dist, Action dir)[] {
				(player.X, Action.Left),
				(Map.Width - 1 - player.X, Action.Right),
				(player.Y, Action.Up),
				(Map.Height - 1 - player.Y, Action.Down)
			}.Aggregate((m, c) => c.dist < m.dist ? c : m).dir;
		}

		private int DistanceToEdge(Action direction)
		{
			MapCoordinate player = MapUtils.GetCoordinateOf(PlayerId);
			switch (direction)
			{
				case Action.Left:
					return player.X;
				case Action.Right:
					return Map.Width - 1 - player.X;
				case Action.Up:
					return player.Y;
				case Action.Down:
					return Map.Height - 1 - player.Y;
				default:
					throw new Exception();
			}
		}

		private Action NextDirection(Action direction) => sideways[direction][0];
	}
}
