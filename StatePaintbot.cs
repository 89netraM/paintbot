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

	public class TileOwner
	{
		public TileType Type { get; set; }
		public string Owner { get; set; }

		public TileOwner(TileType type, string owner) =>
			(Type, Owner) = (type, owner);
	}
	public enum TileType
	{
		Empty,
		Owned,
		Occupied,
	}

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

		private HashSet<MapCoordinate> obstacleCoordinates = null;
		public HashSet<MapCoordinate> ObstacleCoordinates
		{
			get
			{
				if (obstacleCoordinates is null)
				{
					obstacleCoordinates = Map.ObstaclePositions
						.Select(MapUtils.GetCoordinateFrom)
						.ToHashSet();
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
		private HashSet<MapCoordinate> playerColouredCoordinates = null;
		public HashSet<MapCoordinate> PlayerColouredCoordinates
		{
			get
			{
				if (playerColouredCoordinatesCache != Map.WorldTick)
				{
					playerColouredCoordinates = PlayerInfo.ColouredPositions
						.Select(MapUtils.GetCoordinateFrom)
						.ToHashSet();
					playerColouredCoordinatesCache = Map.WorldTick;
				}
				return playerColouredCoordinates;
			}
		}

		private long enemyCoordinatesCache = -1;
		private HashSet<MapCoordinate> enemyCoordinates = null;
		protected HashSet<MapCoordinate> EnemyCoordinates
		{
			get
			{
				if (enemyCoordinatesCache != Map.WorldTick)
				{
					enemyCoordinates = Map.CharacterInfos
						.Where(ci => ci.Id != PlayerId)
						.Select(ci => MapUtils.GetCoordinateFrom(ci.Position))
						.ToHashSet();
					enemyCoordinatesCache = Map.WorldTick;
				}
				return enemyCoordinates;
			}
		}

		private long leadersCache = -1;
		private HashSet<string> leaders = null;
		protected HashSet<string> Leaders
		{
			get
			{
				if (leadersCache != Map.WorldTick)
				{
					leaders = Map.CharacterInfos
						.Where(ci => ci.Id != PlayerId && ci.Points >= PlayerInfo.Points)
						.Select(ci => ci.Id)
						.ToHashSet();
					leadersCache = Map.WorldTick;
				}
				return leaders;
			}
		}

		private Dictionary<MapCoordinate, TileOwner> pointsAtCoordinate = null;
		public IReadOnlyDictionary<MapCoordinate, TileOwner> PointsAtCoordinate => pointsAtCoordinate;

		public MapCoordinate OverrideTarget { get; set; } = null;
		public ISet<MapCoordinate> Disallowed { get; } = new HashSet<MapCoordinate>();
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
			UpdatePointsDictionary();

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
			if (ForceExplode)
			{
				ForceExplode = false;
				if (PlayerInfo.CarryingPowerUp)
				{
					OverrideTarget = null;
					return Action.Explode;
				}
			}
			if (OverrideTarget is not null)
			{
				Path path = Pathfinder.FindPath(this, OverrideTarget.Equals);
				if (path is not null && path.FirstStep != Action.Stay)
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

		private void UpdatePointsDictionary()
		{
			if (pointsAtCoordinate is null)
			{
				pointsAtCoordinate = new Dictionary<MapCoordinate, TileOwner>(Map.Width * Map.Height);
				// Empty coordinates
				for (int y = 0; y < Map.Height; y++)
				{
					for (int x = 0; x < Map.Width; x++)
					{
						MapCoordinate coordinate = new MapCoordinate(x, y);
						if (!ObstacleCoordinates.Contains(coordinate))
						{
							pointsAtCoordinate.Add(coordinate, new TileOwner(TileType.Empty, null));
						}
					}
				}
				// Character coordinates
				foreach (CharacterInfo ci in Map.CharacterInfos)
				{
					pointsAtCoordinate[MapUtils.GetCoordinateFrom(ci.Position)] = new TileOwner(TileType.Occupied, ci.Id);
				}
				_ = EnemyCoordinates;
			}
			else
			{
				// Coloured by the player (old playerCoordinate!)
				pointsAtCoordinate[playerCoordinate].Type = TileType.Owned;
				// Coloured by an enemy (old enemyCoordinates!)
				foreach (MapCoordinate coordinate in enemyCoordinates)
				{
					pointsAtCoordinate[coordinate].Type = TileType.Owned;
				}
				// Coloured by explosion
				foreach (ExplosionInfo explosionInfo in Map.ExplosionInfos)
				{
					MapCoordinate coordinate = MapUtils.GetCoordinateFrom(explosionInfo.Position);
					if (!PlayerCoordinate.Equals(coordinate) &&
						!EnemyCoordinates.Contains(coordinate) &&
						pointsAtCoordinate.GetValueOrDefault(coordinate) is TileOwner tile)
					{
						tile.Type = TileType.Owned;
						if (explosionInfo.Exploders.Length == 1)
						{
							tile.Owner = explosionInfo.Exploders[0];
						}
						else
						{
							int position = MapUtils.GetPositionFrom(coordinate);
							tile.Owner = explosionInfo.Exploders.First(id => MapUtils.GetCharacterInfoFor(id).ColouredPositions.Contains(position));
						}
					}
				}
				// Charcater coordinate
				foreach (CharacterInfo ci in Map.CharacterInfos)
				{
					TileOwner tile = pointsAtCoordinate[MapUtils.GetCoordinateFrom(ci.Position)];
					tile.Type = TileType.Occupied;
					tile.Owner = ci.Id;
				}
			}
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
		public int PointsForUseOfPowerUp(MapCoordinate center, int range) =>
			CoordinatesInManhattanRange(center, range)
				.Sum(CalculatePointsAt);
		public int CalculatePointsAt(MapCoordinate coordinate)
		{
			TileOwner tile = PointsAtCoordinate[coordinate];
			switch (tile.Type)
			{
				default:
				case TileType.Empty:
					return GameSettings.PointsPerTileOwned;
				case TileType.Owned:
					return PlayerId == tile.Owner ? 0 : GameSettings.PointsPerTileOwned * (Leaders.Contains(tile.Owner) ? 2 : 1);
				case TileType.Occupied:
					return GameSettings.PointsPerCausedStun;
			}
		}
	}
}
