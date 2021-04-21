namespace PaintBot.Game.Map
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Action = Action.Action;
	using static Serilog.Log;

	public class MapUtils : IMapUtils
	{
		private readonly IDictionary<string, CharacterInfo> _characterInfoDictionary;
		private readonly CharacterInfo[] _characterInfos;
		private readonly BitArray _characters;
		private readonly CollisionInfo[] _collisionInfos;
		private readonly ExplosionInfo[] _explosionInfos;
		private readonly int _height;
		private readonly int[] _obstaclePositions;
		private readonly BitArray _obstacles;
		private readonly int[] _powerUpPositions;
		private readonly BitArray _powerups;
		private readonly int _width;
		private readonly int _worldTick;

		public MapUtils(Map map)
		{
			_width = map.Width;
			_height = map.Height;
			_worldTick = map.WorldTick;
			_characterInfos = map.CharacterInfos;
			_collisionInfos = map.CollisionInfos;
			_explosionInfos = map.ExplosionInfos;
			_powerUpPositions = map.PowerUpPositions;
			_obstaclePositions = map.ObstaclePositions;

			_characterInfoDictionary = _characterInfos.ToDictionary(c => c.Id);
			_characters = PopulateBitArrayWith(_characterInfos.Select(c => c.Position).ToArray());
			_powerups = PopulateBitArrayWith(_powerUpPositions);
			_obstacles = PopulateBitArrayWith(_obstaclePositions);
		}

		public bool CanPlayerPerformAction(string playerId, Action action)
		{
			try
			{
				var player = _characterInfoDictionary[playerId];
				if (action == Action.Stay)
					return true;
				if (action == Action.Explode)
					return player.CarryingPowerUp;

				var currentPosition = player.Position;
				var coordinate = GetCoordinateFrom(currentPosition);
				var newCoordinate = coordinate.MoveIn(action);

				return IsMovementPossibleTo(newCoordinate);
			}
			catch (Exception e)
			{
				Logger.Error(e, e.Message);
				return false;
			}
		}

		public MapCoordinate[] GetPlayerColoredPositions(string playerId)
		{
			return GetCoordinatesFrom(GetCharacterInfoFor(playerId).ColouredPositions);
		}

		public MapCoordinate[] GetPowerUpCoordinates()
		{
			return GetCoordinatesFrom(_powerUpPositions);
		}

		public MapCoordinate[] GetObstacleCoordinates()
		{
			return GetCoordinatesFrom(_obstaclePositions);
		}

		public MapCoordinate GetCoordinateOf(string playerId)
		{
			return GetCoordinateFrom(GetCharacterInfoFor(playerId).Position);
		}

		public CharacterInfo GetCharacterInfoFor(string playerId)
		{
			var playerExists = _characterInfoDictionary.TryGetValue(playerId, out var player);
			if (!playerExists)
				throw new Exception($"Player with id {playerId} does not exist");

			return player;
		}

		public MapCoordinate GetCoordinateFrom(int position)
		{
			var x = position % _width;
			var y = (position - x) / _width;
			return new MapCoordinate(x, y);
		}

		public MapCoordinate[] GetCoordinatesFrom(int[] positions)
		{
			return positions.Select(GetCoordinateFrom).ToArray();
		}

		public int GetPositionFrom(MapCoordinate coordinate)
		{
			if (coordinate == null)
				throw new ArgumentNullException(nameof(coordinate));

			return coordinate.X + coordinate.Y * _width;
		}

		public Tile GetTileAt(MapCoordinate coordinate)
		{
			if (coordinate == null)
				throw new ArgumentNullException(nameof(coordinate));

			return GetTileAt(GetPositionFrom(coordinate));
		}

		public Tile GetTileAt(int position)
		{
			if (IsPositionOutOfBounds(position))
				throw new Exception($"Position {position} is out of bounds");

			if (_powerups.Get(position))
				return Tile.PowerUp;

			if (_obstacles.Get(position))
				return Tile.Obstacle;

			if (_characters.Get(position))
				return Tile.Character;

			return Tile.Empty;
		}

		public bool IsMovementPossibleTo(MapCoordinate coordinate)
		{
			if (coordinate == null)
				throw new ArgumentNullException(nameof(coordinate));

			return !IsCoordinateOutOfBounds(coordinate) && IsMovementPossibleTo(GetPositionFrom(coordinate));
		}

		public bool IsMovementPossibleTo(int position)
		{
			if (IsPositionOutOfBounds(position))
				return false;

			return !(_obstacles.Get(position) || _characters.Get(position));
		}

		public bool IsCoordinateOutOfBounds(MapCoordinate coordinate)
		{
			if (coordinate == null)
				throw new ArgumentNullException(nameof(coordinate));

			return coordinate.X < 0 || coordinate.X >= _width || coordinate.Y < 0 || coordinate.Y >= _height;
		}

		public bool IsPositionOutOfBounds(int position)
		{
			return position < 0 || position > _height * _width;
		}

		private BitArray PopulateBitArrayWith(int[] positions)
		{
			if (positions == null)
				return new BitArray(0);

			var arr = new BitArray(_height * _width);
			foreach (var pos in positions)
				arr.Set(pos, true);

			return arr;
		}
	}
}
