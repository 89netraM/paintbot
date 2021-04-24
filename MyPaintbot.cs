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
						continue;
					}
					else
					{
						Action[] enemyPath = ShortestPathToAnyEnemy();
						if (enemyPath is not null)
						{
							yield return enemyPath[0];
							continue;
						}
					}
				}

				Action[] powerUpPath = ShortestPathToAnyPowerUp();
				if (powerUpPath is not null)
				{
					yield return powerUpPath[0];
				}
				else
				{
					yield return GetRandomDirection();
				}
			}
		}

		private bool ShouldExplode() =>
			Map.WorldTick == TotalGameTicks - 2 ||
			IsLosingEnemy >= 10 ||
			Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)) < GameSettings.ExplosionRange
			);

		private Action[] ShortestPathToAnyEnemy() =>
			Map.CharacterInfos
				.Where(ci => ci.Id != PlayerId)
				.Select(ci => MapUtils.GetCoordinateFrom(ci.Position))
				.Select(c => Pathfinder.FindPath(this, ct => c.GetManhattanDistanceTo(ct) < GameSettings.ExplosionRange)?.ToArray())
				.OfType<Action[]>()
				.OrderBy(p => p.Length)
				.FirstOrDefault();
		private Action[] ShortestPathToAnyPowerUp() =>
			Map.PowerUpPositions
				.Select(MapUtils.GetCoordinateFrom)
				.Select(c => Pathfinder.FindPath(this, c.Equals)?.ToArray())
				.OfType<Action[]>()
				.OrderBy(p => p.Length)
				.FirstOrDefault();

		private Action GetDirection(MapCoordinate target) =>
			target is not null ? GetDirection(target.Equals) : GetRandomDirection();
		private Action GetDirection(System.Func<MapCoordinate, bool> condition)
		{
			IEnumerable<Action> path = Pathfinder.FindPath(this, condition);
			if (path is not null && path.Any())
			{
				return path.First();
			}
			else
			{
				return GetRandomDirection();
			}
		}

		private Action GetRandomDirection()
		{
			// Go towards the closest coordinate not coloured by this player
			IEnumerable<Action> path = Pathfinder.FindPath(this, c => !PlayerColouredCoordinates.Contains(c));
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
