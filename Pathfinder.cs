namespace PaintBot
{
	using System.Collections.Generic;
	using System.Linq;
	using Game.Action;
	using Game.Map;
	using Priority_Queue;

	public record Path(Action FirstStep, MapCoordinate Target, int Length);

	public static class Pathfinder
	{
		private static readonly IReadOnlyList<Action> directions = new[] { Action.Left, Action.Right, Action.Up, Action.Down };

		public static Path FindPath(StatePaintBot paintBot, System.Func<MapCoordinate, bool> condition)
		{
			if (condition.Invoke(paintBot.PlayerCoordinate))
			{
				return null;
			}

			HashSet<MapCoordinate> otherColouredCoordinates = paintBot.Map.CharacterInfos
				.Where(ci => ci.Id != paintBot.PlayerId)
				.SelectMany(ci => ci.ColouredPositions.Select(paintBot.MapUtils.GetCoordinateFrom))
				.ToHashSet();
			HashSet<MapCoordinate> leaderColouredCoordinates = paintBot.Map.CharacterInfos
				.Where(ci => ci.Id != paintBot.PlayerId && ci.Points >= paintBot.PlayerInfo.Points)
				.SelectMany(ci => ci.ColouredPositions.Select(paintBot.MapUtils.GetCoordinateFrom))
				.ToHashSet();

			ISet<MapCoordinate> visited = new HashSet<MapCoordinate>();
			SimplePriorityQueue<(Path, float), float> toTest = new SimplePriorityQueue<(Path, float), float>();

			toTest.Enqueue((new Path(Action.Stay, paintBot.PlayerCoordinate, 0), 0.0f), 0.0f);

			while (toTest.Count > 0)
			{
				var ((firstStep, from, length), fromSteps) = toTest.Dequeue();
				bool wasInRangeOfOther = IsInRangeOfOther(paintBot, from);
				foreach (Action direction in directions.OrderBy(_ => paintBot.Random.NextDouble()))
				{
					MapCoordinate to = from.MoveIn(direction);
					if (!visited.Contains(to) &&
						!paintBot.Disallowed.Contains(to) &&
						paintBot.MapUtils.IsMovementPossibleTo(to) &&
						!IsTooCloseToOther(paintBot, to) &&
						(wasInRangeOfOther || !IsInRangeOfOther(paintBot, to)))
					{
						float cost = 1.0f - paintBot.CalculatePointsAt(to) / 8.0f + 0.125f;
						Path path = new Path(firstStep != Action.Stay ? firstStep : direction, to, length + 1);
						if (condition.Invoke(to))
						{
							return path;
						}
						visited.Add(to);
						toTest.Enqueue((path, fromSteps + cost), fromSteps + cost);
					}
				}
			}

			return null;
		}

		private static bool IsTooCloseToOther(StatePaintBot paintBot, MapCoordinate coordinate)
		{
			return paintBot.Map.CharacterInfos.Any(ci =>
				ci.Id != paintBot.PlayerId &&
				ci.StunnedForGameTicks == 0 &&
				paintBot.MapUtils.GetCoordinateFrom(ci.Position).GetManhattanDistanceTo(coordinate) <= 1
			);
		}
		private static bool IsInRangeOfOther(StatePaintBot paintBot, MapCoordinate coordinate)
		{
			return paintBot.Map.CharacterInfos.Any(ci =>
				ci.Id != paintBot.PlayerId &&
				ci.StunnedForGameTicks == 0 &&
				ci.CarryingPowerUp &&
				paintBot.MapUtils.GetCoordinateFrom(ci.Position).GetManhattanDistanceTo(coordinate) <=
					paintBot.GameSettings.ExplosionRange
			);
		}
	}
}
