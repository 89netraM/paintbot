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
		protected Map Map { get; private set; }
		protected string PlayerId { get; private set; }
		protected MapUtils MapUtils { get; private set; }

		private long? totalGameTicks = null;
		protected long TotalGameTicks
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

		private long playerInfoCache = -1;
		private CharacterInfo playerInfo = null;
		protected CharacterInfo PlayerInfo
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
		protected MapCoordinate PlayerCoordinate
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
		protected MapCoordinate[] PlayerColouredCoordinates
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

		private IEnumerator<Action> currentActionSequence;

		protected StatePaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender heartBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, heartBeatSender, logger)
		{ }

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

		protected abstract IEnumerable<Action> GetActionSequence();

		public int CountCloseNonPlayerColoured() => CountCloseNonPlayerColoured(PlayerCoordinate, GameSettings.ExplosionRange);
		public int CountCloseNonPlayerColoured(int range) => CountCloseNonPlayerColoured(PlayerCoordinate, range);
		public int CountCloseNonPlayerColoured(MapCoordinate center, int range)
		{
			int count = 0;
			for (int y = -range; y <= range; y++)
			{
				int width = range - System.Math.Abs(y);
				for (int x = -width; x <= width; x++)
				{
					if (x != 0 || y != 0)
					{
						MapCoordinate coordinate = new MapCoordinate(center.X + x, center.Y + y);
						if (MapUtils.IsMovementPossibleTo(coordinate) && !PlayerColouredCoordinates.Contains(coordinate))
						{
							count++;
						}
					}
				}
			}
			return count;
		}
	}
}
