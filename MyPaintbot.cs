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

	public record TargetInfo(MapCoordinate PointsCoordinate, int Points, Path PointsPath, Path PowerUpPath);

	public class MyPaintBot : StatePaintBot
	{
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
				// Always explode on the last tick if possible
				.Select(a => PlayerInfo.CarryingPowerUp && Map.WorldTick == TotalGameTicks - 2 ? Action.Explode : a)
				// Explode if chased
				.Select(a =>
					PlayerInfo.CarryingPowerUp &&
					Map.CharacterInfos.Any(ci =>
						ci.Id != PlayerId &&
						ci.CarryingPowerUp &&
						ci.StunnedForGameTicks == 0 &&
						PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)) <=
							GameSettings.ExplosionRange
					) ? Action.Explode : a
				)
				// Never do nothing
				.Select(pa => pa is Action a ? a : GetRandomAction());

		private Action GetRandomAction()
		{
			int leftBorder = Map.Width / 4;
			int rightBorder = Map.Width - leftBorder;
			int topBorder = Map.Height / 4;
			int bottomBorder = Map.Height - topBorder;
			return Pathfinder.FindPath(
				this,
				c =>
					// Never target already owned tiles
					!PlayerColouredCoordinates.Contains(c) &&
					// Prefer setping to the center
					leftBorder <= c.X && c.X <= rightBorder && topBorder <= c.Y && c.Y <= bottomBorder &&
					// Makes it prefer steping on others colours
					PlayerCoordinate.GetManhattanDistanceTo(c) >= 2 &&
					// Don't move into dead-ends
					CountCloseNonPlayerColoured(c, 1) >= 2
			)?.FirstStep ?? Action.Stay;
		}

		private IEnumerable<Action?> GetPreliminaryActionSequence()
		{
			while (true)
			{
				// Find the closest power-up
				while (!PlayerInfo.CarryingPowerUp)
				{
					if (Map.PowerUpPositions.Length != 0 &&
						Pathfinder.FindPath(this, IsPowerUp) is Path p)
					{
						yield return p.FirstStep;
					}
					else
					{
						yield return null;
					}
				}
				// Find a nearby "explosion point" and a path to the next power-up
				TargetInfo target = FindTarget();
				Path toPoints = target.PointsPath;
				while (PlayerInfo.CarryingPowerUp)
				{
					if (toPoints?.Coordinates.Count == 0)
					{
						yield return Action.Explode;
						break;
					}
					else
					{
						yield return toPoints?.FirstStep;
					}
					if (IsTargetOccupied(target.PointsCoordinate))
					{
						target = FindTarget();
					}
					toPoints = Pathfinder.FindPath(this, target.PointsCoordinate.Equals);
				}
			}
		}

		private bool IsPowerUp(MapCoordinate coordinate) =>
			Map.PowerUpPositions.Contains(MapUtils.GetPositionFrom(coordinate));

		private bool IsTargetOccupied(MapCoordinate coordinate) =>
			Map.CharacterInfos.Any(ci =>
			{
				if (ci.Id == PlayerId)
				{
					return false;
				}
				MapCoordinate characterCoordinate = MapUtils.GetCoordinateFrom(ci.Position);
				if (ci.StunnedForGameTicks > 0 && characterCoordinate.GetManhattanDistanceTo(coordinate) <= 1)
				{
					return true;
				}
				else if (ci.CarryingPowerUp && characterCoordinate.GetManhattanDistanceTo(coordinate) < GameSettings.ExplosionRange)
				{
					return true;
				}
				else
				{
					return false;
				}
			});

		private TargetInfo FindTarget() =>
			CoordinatesInManhattanRange(GameSettings.ExplosionRange * 2)
				.AsParallel()
				.Select(CalculateCoordinate)
				.Where(TargetInfoIsValid)
				.Aggregate(AggregateHighestPointsperStep);
		private TargetInfo CalculateCoordinate(MapCoordinate coordinate) =>
			new TargetInfo(
				coordinate,
				PointsForUseOfPowerUp(coordinate, GameSettings.ExplosionRange),
				Pathfinder.FindPath(this, coordinate.Equals),
				Pathfinder.FindPath(this, coordinate, IsPowerUp)
			);
		private static bool TargetInfoIsValid(TargetInfo targetInfo) =>
			targetInfo.PointsPath is not null;
		private static TargetInfo AggregateHighestPointsperStep(TargetInfo accumulated, TargetInfo targetInfo) =>
			accumulated.Points / (float)(accumulated.PointsPath.Coordinates.Count + (accumulated.PowerUpPath?.Coordinates.Count ?? 0.0f)) <
				targetInfo.Points / (float)(targetInfo.PointsPath.Coordinates.Count + (targetInfo.PowerUpPath?.Coordinates.Count ?? 0.0f)) ?
					targetInfo : accumulated;
	}
}
