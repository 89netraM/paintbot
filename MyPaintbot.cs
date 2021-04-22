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
		private static readonly IReadOnlyDictionary<Action, (Action left, Action right, Action back)> sideways = new Dictionary<Action, (Action, Action, Action)>
		{
			{ Action.Left, (Action.Down, Action.Up, Action.Right) },
			{ Action.Right, (Action.Up, Action.Down, Action.Left) },
			{ Action.Up, (Action.Left, Action.Right, Action.Down) },
			{ Action.Down, (Action.Right, Action.Left, Action.Up) },
		};

		public MyPaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender hearBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, hearBeatSender, logger)
		{
			GameMode = paintBotConfig.GameMode;
			Name = paintBotConfig.Name ?? "My c# bot";
		}

		public override GameMode GameMode { get; }

		public override string Name { get; }

		protected override IEnumerable<Action> GetActionSequence() =>
			GetPreliminaryActionSequence()
				.SelectMany(AvoidOthers)
				.SelectMany(UsePowerUp);

		private IEnumerable<Action> AvoidOthers(Action proposedAction)
		{
			if (IsDirection(proposedAction))
			{
				if (CanAnyOtherGoTo(PlayerCoordinate.MoveIn(proposedAction)))
				{
					if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[proposedAction].right) &&
						!CanAnyOtherGoTo(PlayerCoordinate.MoveIn(sideways[proposedAction].right)))
					{
						yield return sideways[proposedAction].right;
					}
					else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[proposedAction].left) &&
						!CanAnyOtherGoTo(PlayerCoordinate.MoveIn(sideways[proposedAction].left)))
					{
						yield return sideways[proposedAction].left;
					}
					else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[proposedAction].back) &&
						!CanAnyOtherGoTo(PlayerCoordinate.MoveIn(sideways[proposedAction].back)))
					{
						yield return sideways[proposedAction].back;
					}
					else
					{
						yield return Action.Stay;
					}
				}
				else
				{
					yield return proposedAction;
				}
			}
			else
			{
				yield return proposedAction;
			}
		}

		private IEnumerable<Action> UsePowerUp(Action proposedAction)
		{
			if (PlayerInfo.CarryingPowerUp)
			{
				int durationInTicks = GameSettings.GameDurationInSeconds * 1000 / GameSettings.TimeInMsPerTick;
				if (Map.WorldTick == durationInTicks - 2 || // Last tick
					IsAnyInRangeOfExplosion() ||
					WillTakePowerUp(proposedAction))
				{
					yield return Action.Explode;
				}
			}

			yield return proposedAction;
		}

		private IEnumerable<Action> GetPreliminaryActionSequence()
		{
			// Find and go to closest edge
			Action direction = DirectionOfClosestEdge();
			while (DistanceToEdge(direction) > 0)
			{
				if (!PlayerColouredCoordinates.Contains(PlayerCoordinate.MoveIn(direction)) &&
					MapUtils.CanPlayerPerformAction(PlayerId, direction))
				{
					yield return direction;
				}
				else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].right))
				{
					yield return sideways[direction].right;
				}
				else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].left))
				{
					yield return sideways[direction].left;
				}
				else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].back))
				{
					yield return sideways[direction].back;
				}
				else
				{
					yield return Action.Stay;
				}
			}
			// Go Around
			while (true)
			{
				direction = NextDirection(direction);
				while (DistanceToEdge(direction) > 0)
				{
					if (!PlayerColouredCoordinates.Contains(PlayerCoordinate.MoveIn(sideways[direction].left))
						&& MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].left))
					{
						yield return sideways[direction].left;
					}
					else if (!PlayerColouredCoordinates.Contains(PlayerCoordinate.MoveIn(direction)) &&
						MapUtils.CanPlayerPerformAction(PlayerId, direction))
					{
						yield return direction;
					}
					else if (!PlayerColouredCoordinates.Contains(PlayerCoordinate.MoveIn(sideways[direction].right)) &&
						MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].right))
					{
						yield return sideways[direction].right;
					}
					else if (!PlayerColouredCoordinates.Contains(PlayerCoordinate.MoveIn(sideways[direction].back)) &&
						MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].back))
					{
						yield return sideways[direction].back;
					}
					else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].right))
					{
						yield return sideways[direction].right;
					}
					else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].back))
					{
						yield return sideways[direction].back;
					}
					else
					{
						yield return Action.Stay;
					}
				}
			}
		}

		private Action DirectionOfClosestEdge()
		{
			return new (int dist, Action dir)[] {
				(PlayerCoordinate.X, Action.Left),
				(Map.Width - 1 - PlayerCoordinate.X, Action.Right),
				(PlayerCoordinate.Y, Action.Up),
				(Map.Height - 1 - PlayerCoordinate.Y, Action.Down)
			}.Aggregate((m, c) => c.dist < m.dist ? c : m).dir;
		}

		private int DistanceToEdge(Action direction)
		{
			switch (direction)
			{
				case Action.Left:
					return PlayerCoordinate.X;
				case Action.Right:
					return Map.Width - 1 - PlayerCoordinate.X;
				case Action.Up:
					return PlayerCoordinate.Y;
				case Action.Down:
					return Map.Height - 1 - PlayerCoordinate.Y;
				default:
					throw new Exception();
			}
		}

		private Action NextDirection(Action direction) => sideways[direction].right;

		private bool CanAnyOtherGoTo(MapCoordinate coordinate)
		{
			return Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				ci.StunnedForGameTicks == 0 &&
				MapUtils.GetCoordinateFrom(ci.Position).GetManhattanDistanceTo(coordinate) == 1
			);
		}

		private bool IsDirection(Action action) =>
			(Action.Left | Action.Right | Action.Up | Action.Down).HasFlag(action);

		private bool IsAnyInRangeOfExplosion()
		{
			return Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)) <= GameSettings.ExplosionRange
			);
		}

		private bool WillTakePowerUp(Action action) =>
			IsDirection(action) &&
			MapUtils.GetTileAt(PlayerCoordinate.MoveIn(action)) == Tile.PowerUp;
	}
}
