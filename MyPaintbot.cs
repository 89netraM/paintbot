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

		private long closestEnemyPathCache = -1;
		private Action[] closestEnemyPath = null;
		public Action[] ClosestEnemyPath
		{
			get
			{
				if (closestEnemyPathCache != Map.WorldTick)
				{
					closestEnemyPath = Pathfinder.FindPath(
						this,
						c => EnemyCoordinates.Any(ec => c.GetManhattanDistanceTo(ec) < GameSettings.ExplosionRange)
					)?.ToArray();
					closestEnemyPathCache = Map.WorldTick;
				}
				return closestEnemyPath;
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
						Action[] enemyPath = ClosestEnemyPath;
						if (enemyPath is not null && enemyPath.Length > 0)
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
							yield return enemyPath[0];
							continue;
						}
					}
				}

				Action[] powerUpPath = ClosestPowerUpPath;
				if (powerUpPath is not null && powerUpPath.Length > 0)
				{
					yield return powerUpPath.First();
				}
				else
				{
					yield return GetRandomDirection();
				}
			}
		}

		private bool ShouldExplode() =>
			Map.WorldTick == TotalGameTicks - 2 ||
			(IsLosingEnemy >= 10 && CountCloseNonPlayerColoured() >= GameSettings.PointsPerCausedStun * 2) ||
			ClosestPowerUpPath?.Length <= 2 ||
			Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)) < GameSettings.ExplosionRange
			);

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
