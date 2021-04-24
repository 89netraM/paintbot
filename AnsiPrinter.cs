namespace PaintBot
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Game.Map;

	public class AnsiPrinter
	{
		private static readonly char BlockCharacter = '▄';
		private static readonly byte TileColour = 15;
		private static readonly byte ObstacleColour = 232;
		private static readonly byte PowerUpColour = 11;
		private static readonly IReadOnlyList<Colour> Colours = new[]
		{
			new Colour(196, 197),
			new Colour(21, 20),
			new Colour(34, 40),
			new Colour(202, 208),
			new Colour(51, 123),
			new Colour(201, 165),
			new Colour(250, 254),
			new Colour(213, 177),
			new Colour(4, 12),
			new Colour(226, 227),
		};

		public bool IsSetup { get; private set; } = false;
		private readonly IDictionary<string, int> playerColours = new Dictionary<string, int>();

		public void SetupPlayers(string playerId, CharacterInfo[] players)
		{
			if (!IsSetup)
			{
				string[] playerIds = players.Select(ci => ci.Id).Where(id => id != playerId).ToArray();
				for (int i = 0; i < playerIds.Length; i++)
				{
					playerColours[playerIds[i]] = i + 1;
				}
				playerColours[playerId] = 0;
				IsSetup = true;
			}
		}

		public string WriteMap(Map map)
		{
			MapUtils mapUtils = new MapUtils(map);
			IDictionary<MapCoordinate, string> players = map.CharacterInfos
				.ToDictionary(ci => mapUtils.GetCoordinateFrom(ci.Position), ci => ci.Id);
			ISet<MapCoordinate> powerUps = mapUtils.GetPowerUpCoordinates().ToHashSet();
			IDictionary<MapCoordinate, string> paintedTiles = map.CharacterInfos
				.SelectMany(ci => ci.ColouredPositions
					.Select(p => (c: mapUtils.GetCoordinateFrom(p), id: ci.Id))
				)
				.ToDictionary(cid => cid.c, c => c.id);
			ISet<MapCoordinate> obstacles = mapUtils.GetObstacleCoordinates().ToHashSet();

			byte GetColour(MapCoordinate coordinate)
			{
				string id;
				if (players.TryGetValue(coordinate, out id))
				{
					return Colours[playerColours[id]].Player;
				}
				else if (powerUps.Contains(coordinate))
				{
					return PowerUpColour;
				}
				else if (paintedTiles.TryGetValue(coordinate, out id))
				{
					return Colours[playerColours[id]].Tile;
				}
				else if (obstacles.Contains(coordinate))
				{
					return ObstacleColour;
				}
				else
				{
					return TileColour;
				}
			}

			StringBuilder sb = new StringBuilder();
			byte prevForeground = TileColour;
			byte prevBackground = ObstacleColour;
			for (int y = 0; y < map.Height; y += 2)
			{
				for (int x = 0; x < map.Width; x++)
				{
					byte foreground = GetColour(new MapCoordinate(x, y + 1));
					byte background = GetColour(new MapCoordinate(x, y));
					if (prevForeground != foreground)
					{
						sb.Append($"\x1b[38;5;{foreground}m");
						prevForeground = foreground;
					}
					if (prevBackground != background)
					{
						sb.Append($"\x1b[48;5;{background}m");
						prevBackground = background;
					}
					sb.Append(BlockCharacter);
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}

	readonly struct Colour
	{
		public byte Player { get; }
		public byte Tile { get; }

		public Colour(byte player, byte tile)
		{
			Player = player;
			Tile = tile;
		}
	}
}
