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
			GetPreliminaryActionSequence().SelectMany(AvoidOthers);

		private IEnumerable<Action> AvoidOthers(Action proposedAction)
		{
			if ((Action.Left | Action.Right | Action.Up | Action.Down).HasFlag(proposedAction))
			{
				MapCoordinate player = MapUtils.GetCoordinateOf(PlayerId);
				if (CanAnyOtherGoTo(player.MoveIn(proposedAction)))
				{
					if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[proposedAction].right) &&
						!CanAnyOtherGoTo(player.MoveIn(sideways[proposedAction].right)))
					{
						yield return sideways[proposedAction].right;
					}
					else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[proposedAction].left) &&
						!CanAnyOtherGoTo(player.MoveIn(sideways[proposedAction].left)))
					{
						yield return sideways[proposedAction].left;
					}
					else if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[proposedAction].back) &&
						!CanAnyOtherGoTo(player.MoveIn(sideways[proposedAction].back)))
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

		private IEnumerable<Action> GetPreliminaryActionSequence()
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
					if (MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].right))
					{
						yield return sideways[direction].right;
					}
					else
					{
						yield return sideways[direction].left;
					}
				}
			}
			// Go Around
			while (true)
			{
				direction = NextDirection(direction);
				while (DistanceToEdge(direction) > 0)
				{
					CharacterInfo ci = MapUtils.GetCharacterInfoFor(PlayerId);
					MapCoordinate[] owned = MapUtils.GetCoordinatesFrom(ci.ColouredPositions);
					if (!owned.Contains(MapUtils.GetCoordinateOf(PlayerId).MoveIn(sideways[direction].left))
						&& MapUtils.CanPlayerPerformAction(PlayerId, sideways[direction].left))
					{
						yield return sideways[direction].left;
					}
					else if (!owned.Contains(MapUtils.GetCoordinateOf(PlayerId).MoveIn(direction)) &&
						MapUtils.CanPlayerPerformAction(PlayerId, direction))
					{
						yield return direction;
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

		private Action NextDirection(Action direction) => sideways[direction].right;

		private bool CanAnyOtherGoTo(MapCoordinate coordinate)
		{
			return Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				MapUtils.GetCoordinateFrom(ci.Position).GetManhattanDistanceTo(coordinate) == 1
			);
		}
	}
}
