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

	public abstract class StatePaintBot : PaintBot
	{
		public System.Random Random { get; }
		public Map Map { get; private set; }
		public string PlayerId { get; private set; }
		public MapUtils MapUtils { get; private set; }

		private long? totalGameTicks = null;
		public long TotalGameTicks
		{
			get
			{
				if (totalGameTicks.HasValue)
				{
					return totalGameTicks.Value;
				}
				else
				{
					totalGameTicks = GameSettings.GameDurationInSeconds * 1000 / GameSettings.TimeInMsPerTick;
					return totalGameTicks.Value;
				}
			}
		}

		private MapCoordinate[] obstacleCoordinates = null;
		public MapCoordinate[] ObstacleCoordinates
		{
			get
			{
				if (obstacleCoordinates is null)
				{
					obstacleCoordinates = MapUtils.GetObstacleCoordinates();
				}
				return obstacleCoordinates;
			}
		}

		private long playerInfoCache = -1;
		private CharacterInfo playerInfo = null;
		public CharacterInfo PlayerInfo
		{
			get
			{
				if (playerInfoCache != Map.WorldTick)
				{
					playerInfo = MapUtils.GetCharacterInfoFor(PlayerId);
					playerInfoCache = Map.WorldTick;
				}
				return playerInfo;
			}
		}

		private long playerCoordinateCache = -1;
		private MapCoordinate playerCoordinate = null;
		public MapCoordinate PlayerCoordinate
		{
			get
			{
				if (playerCoordinateCache != Map.WorldTick)
				{
					playerCoordinate = MapUtils.GetCoordinateFrom(PlayerInfo.Position);
					playerCoordinateCache = Map.WorldTick;
				}
				return playerCoordinate;
			}
		}

		private long playerColouredCoordinatesCache = -1;
		private MapCoordinate[] playerColouredCoordinates = null;
		public MapCoordinate[] PlayerColouredCoordinates
		{
			get
			{
				if (playerColouredCoordinatesCache != Map.WorldTick)
				{
					playerColouredCoordinates = MapUtils.GetCoordinatesFrom(PlayerInfo.ColouredPositions);
					playerColouredCoordinatesCache = Map.WorldTick;
				}
				return playerColouredCoordinates;
			}
		}

		private long enemyCoordinatesCache = -1;
		private MapCoordinate[] enemyCoordinates = null;
		protected MapCoordinate[] EnemyCoordinates
		{
			get
			{
				if (enemyCoordinatesCache != Map.WorldTick)
				{
					enemyCoordinates = Map.CharacterInfos
						.Where(ci => ci.Id != PlayerId)
						.Select(ci => MapUtils.GetCoordinateFrom(ci.Position))
						.ToArray();
					enemyCoordinatesCache = Map.WorldTick;
				}
				return enemyCoordinates;
			}
		}

		public MapCoordinate OverrideTarget { get; set; } = null;
		public bool ForceExplode { get; set; } = false;

		private IEnumerator<Action> currentActionSequence;

		protected StatePaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender heartBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, heartBeatSender, logger)
		{
			Random = new System.Random();
		}

		public override Action GetAction(MapUpdated mapUpdated)
		{
			Map = mapUpdated.Map;
			PlayerId = mapUpdated.ReceivingPlayerId;
			MapUtils = new MapUtils(Map);

			if (currentActionSequence is null || !currentActionSequence.MoveNext())
			{
				currentActionSequence = GetActionSequence().GetEnumerator();
				currentActionSequence.MoveNext();
			}

			return currentActionSequence.Current;
		}

		public override Action? GetOverrideAction()
		{
			if (currentActionSequence.Current == Action.Explode)
			{
				OverrideTarget = null;
			}
			if (ForceExplode) {
				ForceExplode = false;
				OverrideTarget = null;
				return Action.Explode;
			}
			if (OverrideTarget is not null)
			{
				Path path = Pathfinder.FindPath(this, OverrideTarget.Equals);
				if (path is not null)
				{
					return path.FirstStep;
				}
				else
				{
					OverrideTarget = null;
				}
			}

			return null;
		}

		protected abstract IEnumerable<Action> GetActionSequence();

		public IEnumerable<MapCoordinate> CoordinatesInManhattanRange() => CoordinatesInManhattanRange(GameSettings.ExplosionRange);
		public IEnumerable<MapCoordinate> CoordinatesInManhattanRange(int range) => CoordinatesInManhattanRange(PlayerCoordinate, range);
		public IEnumerable<MapCoordinate> CoordinatesInManhattanRange(MapCoordinate center, int range)
		{
			for (int y = -range; y <= range; y++)
			{
				int width = range - System.Math.Abs(y);
				for (int x = -width; x <= width; x++)
				{
					if (x != 0 || y != 0)
					{
						MapCoordinate coordinate = new MapCoordinate(center.X + x, center.Y + y);
						if (!MapUtils.IsCoordinateOutOfBounds(coordinate) && !ObstacleCoordinates.Contains(coordinate))
						{
							yield return coordinate;
						}
					}
				}
			}
		}

		public int CountCloseNonPlayerColoured() => CountCloseNonPlayerColoured(GameSettings.ExplosionRange);
		public int CountCloseNonPlayerColoured(int range) => CountCloseNonPlayerColoured(PlayerCoordinate, range);
		public int CountCloseNonPlayerColoured(MapCoordinate center, int range) =>
			CoordinatesInManhattanRange(center, range)
			.Count(c => !PlayerColouredCoordinates.Contains(c));

		public int PointsForUseOfPowerUp() => PointsForUseOfPowerUp(GameSettings.ExplosionRange);
		public int PointsForUseOfPowerUp(int range) => PointsForUseOfPowerUp(PlayerCoordinate, range);
		public int PointsForUseOfPowerUp(MapCoordinate center, int range)
		{
			// Coordinates coloured by other players with more or equal amount
			// of points
			HashSet<MapCoordinate> leaderColouredCoordinates = Map.CharacterInfos
				.Where(ci => ci.Id != PlayerId && ci.Points >= PlayerInfo.Points)
				.SelectMany(ci => ci.ColouredPositions.Select(MapUtils.GetCoordinateFrom))
				.ToHashSet();

			return CoordinatesInManhattanRange(center, range)
				.Sum(coordinate =>
				{
					if (MapUtils.GetTileAt(coordinate) == Tile.Character)
					{
						return GameSettings.PointsPerCausedStun;
					}
					else if (leaderColouredCoordinates.Contains(coordinate))
					{
						return GameSettings.PointsPerTileOwned * 2;
					}
					else
					{
						return GameSettings.PointsPerTileOwned;
					}
				});
		}
	}
}
