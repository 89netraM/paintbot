namespace PaintBot
{
	using System.Collections.Generic;
	using System.Linq;
	using Game.Action;
	using Game.Map;
	using Priority_Queue;

	public static class Pathfinder
	{
		private static readonly IReadOnlyList<Action> directions = new[] { Action.Left, Action.Right, Action.Up, Action.Down };

		public static IEnumerable<Action> FindPath(MyPaintBot paintBot, System.Func<MapCoordinate, bool> condition)
		{
			if (condition.Invoke(paintBot.PlayerCoordinate))
			{
				return Enumerable.Empty<Action>();
			}

			Dictionary<MapCoordinate, Action?> directionTo = new Dictionary<MapCoordinate, Action?>();
			SimplePriorityQueue<(MapCoordinate, float), float> toTest = new SimplePriorityQueue<(MapCoordinate, float), float>();

			directionTo.Add(paintBot.PlayerCoordinate, null);
			toTest.Enqueue((paintBot.PlayerCoordinate, 0.0f), 0.0f);

			while (toTest.Count > 0)
			{
				var (from, fromSteps) = toTest.Dequeue();
				foreach (Action direction in directions.OrderBy(_ => paintBot.Random.NextDouble()))
				{
					MapCoordinate to = from.MoveIn(direction);
					if (!directionTo.ContainsKey(to) &&
						paintBot.MapUtils.IsMovementPossibleTo(to) &&
						!IsTooCloseToOther(paintBot, to))
					{
						float cost = 1.0f;
						if (paintBot.PlayerColouredCoordinates.Contains(to))
						{
							cost += 0.25f;
						}
						directionTo.Add(to, direction);
						if (condition.Invoke(to))
						{
							return BuildPath(directionTo, to);
						}
						toTest.Enqueue((to, fromSteps + cost), fromSteps + cost);
					}
				}
			}

			return null;
		}

		private static bool IsTooCloseToOther(MyPaintBot paintBot, MapCoordinate coordinate)
		{
			return paintBot.Map.CharacterInfos.Any(ci =>
				ci.Id != paintBot.PlayerId &&
				ci.StunnedForGameTicks == 0 &&
				paintBot.MapUtils.GetCoordinateFrom(ci.Position).GetManhattanDistanceTo(coordinate) <=
					(ci.CarryingPowerUp ? paintBot.GameSettings.ExplosionRange : 1)
			);
		}

		private static IEnumerable<Action> BuildPath(IReadOnlyDictionary<MapCoordinate, Action?> directionTo, MapCoordinate to)
		{
			if (directionTo.TryGetValue(to, out Action? direction) && direction.HasValue)
			{
				IEnumerable<Action> directions = BuildPath(directionTo, to.MoveIn(Reverse(direction.Value)));
				return directions.Append(direction.Value);
			}
			else
			{
				return Enumerable.Empty<Action>();
			}
		}

		private static Action Reverse(Action forward) => forward switch
		{
			Action.Left => Action.Right,
			Action.Right => Action.Left,
			Action.Up => Action.Down,
			Action.Down => Action.Up,
			_ => throw new System.Exception()
		};
	}
}
