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
						Pathfinder.FindPath(this, c => Map.PowerUpPositions.Contains(MapUtils.GetPositionFrom(c))) is Path p)
					{
						yield return p.FirstStep;
					}
					else
					{
						yield return null;
					}
				}
				// Seek out most valuable path through an "explosion" point to a power-up
				while (PlayerInfo.CarryingPowerUp)
				{
					if (Map.PowerUpPositions.Length != 0 &&
						Pathfinder.FindPath(this, c => Map.PowerUpPositions.Contains(MapUtils.GetPositionFrom(c))) is Path p)
					{
						if (p.Coordinates.Count == 1 || IsAtBestInPath(p))
						{
							yield return Action.Explode;
						}
						else
						{
							yield return p.FirstStep;
						}
					}
					else
					{
						yield return null;
					}
				}
			}
		}

		private bool IsAtBestInPath(Path path)
		{
			int pointsHere = PointsForUseOfPowerUp(PlayerCoordinate, GameSettings.ExplosionRange);
			return path.Coordinates.All(c => PointsForUseOfPowerUp(c, GameSettings.ExplosionRange) - GameSettings.ExplosionRange / 2 <= pointsHere);
		}
	}
}
