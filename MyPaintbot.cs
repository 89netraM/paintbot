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

	public class MyPaintBot : StatePaintBot
	{
		public readonly System.Random Random;
		public int? DistanceToEnemy { get; private set; } = null;
		public int IsLosingEnemy { get; private set; } = 0;

		private long closestExplodePathCache = -1;
		private Action[] closestExplodePath = null;
		public Action[] ClosestExplodePath
		{
			get
			{
				if (closestExplodePathCache != Map.WorldTick)
				{
					closestExplodePath = Pathfinder.FindPath(
						this,
						c => PointsForUseOfPowerUp(c, GameSettings.ExplosionRange) >= (GameSettings.ExplosionRange + 1.0) * (GameSettings.ExplosionRange * 0.5) * 4.0
					)?.ToArray();
					closestExplodePathCache = Map.WorldTick;
				}
				return closestExplodePath;
			}
		}

		private long closestPowerUpPathCache = -1;
		private Action[] closestPowerUpPath = null;
		public Action[] ClosestPowerUpPath
		{
			get
			{
				if (closestPowerUpPathCache != Map.WorldTick)
				{
					closestPowerUpPath = Pathfinder.FindPath(
						this,
						c => Map.PowerUpPositions.Contains(MapUtils.GetPositionFrom(c))
					)?.ToArray();
					closestPowerUpPathCache = Map.WorldTick;
				}
				return closestPowerUpPath;
			}
		}

		public MyPaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender hearBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, hearBeatSender, logger)
		{
			GameMode = paintBotConfig.GameMode;
			Name = paintBotConfig.Name ?? "My c# bot";

			Random = new System.Random();
		}

		public override GameMode GameMode { get; }

		public override string Name { get; }

		protected override IEnumerable<Action> GetActionSequence() =>
			GetPreliminaryActionSequence()
				// Always explode if an enemy is in range
				.Select(a => PlayerInfo.CarryingPowerUp && EnemyCoordinates.Any(c => c.GetManhattanDistanceTo(PlayerCoordinate) < GameSettings.ExplosionRange) ? Action.Explode : a)
				// Always try to explode on last tick
				.Select(a => PlayerInfo.CarryingPowerUp && Map.WorldTick == TotalGameTicks - 2 ? Action.Explode : a);

		private IEnumerable<Action> GetPreliminaryActionSequence()
		{
			while (!PlayerInfo.CarryingPowerUp)
			{
				if (ClosestPowerUpPath is not null)
				{
					yield return ClosestPowerUpPath.First();
				}
				else
				{
					yield return GetRandomDirection();
				}
			}

			while (PlayerInfo.CarryingPowerUp)
			{
				if (ClosestExplodePath?.Length > 0)
				{
					yield return ClosestExplodePath.First();
				}
				else
				{
					yield return Action.Explode;
				}
			}
		}

		private Action GetRandomDirection()
		{
			// Go towards the closest coordinate not coloured by this player
			IEnumerable<Action> path = Pathfinder.FindPath(
				this,
				c => !PlayerColouredCoordinates.Contains(c) &&
					CountCloseNonPlayerColoured(c, 1) >= 2
			);
			if (path is not null && path.Any())
			{
				return path.First();
			}
			else
			{
				return Action.Stay;
			}
		}
	}
}
