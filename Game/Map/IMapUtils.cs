namespace PaintBot.Game.Map
{
	using Action = Action.Action;

	public interface IMapUtils
	{
		/// <summary>
		/// Checks whether the given action can be performed on the current map.
		/// </summary>
		bool CanPlayerPerformAction(string playerId, Action action);

		/// <summary>
		/// Returns an array of coordinates at which the given player has colored tiles on the current map.
		/// </summary>
		MapCoordinate[] GetPlayerColoredPositions(string playerId);

		/// <summary>
		/// Returns an array of coordinates at which there are power ups on the current map.
		/// </summary>
		MapCoordinate[] GetPowerUpCoordinates();

		/// <summary>
		/// Returns an array of coordinates at which there are Obstacles on the current map.
		/// </summary>
		MapCoordinate[] GetObstacleCoordinates();

		/// <summary>
		/// Returns the coordinate of the given player
		/// </summary>
		MapCoordinate GetCoordinateOf(string playerId);

		/// <summary>
		/// Returns info about the given player's character
		/// </summary>
		CharacterInfo GetCharacterInfoFor(string playerId);

		/// <summary>
		/// Converts the given position to a map coordinate
		/// </summary>
		MapCoordinate GetCoordinateFrom(int position);

		/// <summary>
		/// Converts the given positions to map coordinates
		/// </summary>
		MapCoordinate[] GetCoordinatesFrom(int[] positions);

		/// <summary>
		/// Converts the given map coordinate to a position
		/// </summary>
		int GetPositionFrom(MapCoordinate coordinate);

		/// <summary>
		/// Returns what type of tile is present at the given coordinate
		/// </summary>
		Tile GetTileAt(MapCoordinate coordinate);

		/// <summary>
		/// Returns what type of tile is present at the given position
		/// </summary>
		Tile GetTileAt(int position);

		/// <summary>
		/// Checks whether it's possible to move to a given coordinate
		/// </summary>
		bool IsMovementPossibleTo(MapCoordinate coordinate);

		/// <summary>
		/// Checks whether it's possible to move to a given position
		/// </summary>
		bool IsMovementPossibleTo(int position);

		/// <summary>
		/// Checks whether a coordinate is outside the boundaries of the map
		/// </summary>
		bool IsCoordinateOutOfBounds(MapCoordinate coordinate);

		/// <summary>
		/// Checks whether a position is outside the boundaries of the map
		/// </summary>
		bool IsPositionOutOfBounds(int position);

	}
}
