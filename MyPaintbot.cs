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
		public int? DistanceToEnemy { get; private set; } = null;
		public int IsLosingEnemy { get; private set; } = 0;

		private long closestEnemyPathCache = -1;
		private Path closestEnemyPath = null;
		public Path ClosestEnemyPath
		{
			get
			{
				if (closestEnemyPathCache != Map.WorldTick)
				{
					closestEnemyPath = Pathfinder.FindPath(
						this,
						c => EnemyCoordinates.Any(ec => c.GetManhattanDistanceTo(ec) < GameSettings.ExplosionRange)
					);
					closestEnemyPathCache = Map.WorldTick;
				}
				return closestEnemyPath;
			}
		}

		private long closestPowerUpPathCache = -1;
		private Path closestPowerUpPath = null;
		public Path ClosestPowerUpPath
		{
			get
			{
				if (closestPowerUpPathCache != Map.WorldTick)
				{
					closestPowerUpPath = Pathfinder.FindPath(
						this,
						c => Map.PowerUpPositions.Contains(MapUtils.GetPositionFrom(c))
					);
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
		}

		public override GameMode GameMode { get; }

		public override string Name { get; }

		protected override IEnumerable<Action> GetActionSequence()
		{
			while (true)
			{
				if (PlayerInfo.CarryingPowerUp)
				{
					if (ShouldExplode())
					{
						yield return Action.Explode;
						DistanceToEnemy = null;
						IsLosingEnemy = 0;
						continue;
					}
					else
					{
						Path enemyPath = ClosestEnemyPath;
						if (enemyPath is not null)
						{
							if (DistanceToEnemy.HasValue && DistanceToEnemy.Value <= enemyPath.Length)
							{
								IsLosingEnemy++;
							}
							else
							{
								IsLosingEnemy = 0;
							}
							DistanceToEnemy = enemyPath.Length;
							yield return enemyPath.FirstStep;
							continue;
						}
					}
				}

				Path powerUpPath = ClosestPowerUpPath;
				if (powerUpPath is not null)
				{
					yield return powerUpPath.FirstStep;
				}
				else
				{
					yield return GetRandomDirection();
				}
			}
		}

		private bool ShouldExplode() =>
			Map.WorldTick == TotalGameTicks - 2 ||
			(IsLosingEnemy >= 10 &&
				PointsForUseOfPowerUp() >= (GameSettings.ExplosionRange + 1.0) * (GameSettings.ExplosionRange * 0.5) * 4 / 2
			) ||
			ClosestPowerUpPath?.Length <= 2 ||
			Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)) < GameSettings.ExplosionRange
			);

		private Action GetRandomDirection()
		{
			// Go towards the closest coordinate not coloured by this player
			Path path = Pathfinder.FindPath(
				this,
				c => !PlayerColouredCoordinates.Contains(c) &&
					CountCloseNonPlayerColoured(c, 1) >= 2
			);
			if (path is not null)
			{
				return path.FirstStep;
			}
			else
			{
				return Action.Stay;
			}
		}
	}
}
