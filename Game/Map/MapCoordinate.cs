namespace PaintBot.Game.Map
{
	using System;
	using Action = Action.Action;

	public class MapCoordinate
	{
		public int X { get; }

		public int Y { get; }

		public MapCoordinate(int x, int y) => (X, Y) = (x, y);

		public MapCoordinate MoveIn(Action action) => action switch
		{
			Action.Down => Move(0, 1),
			Action.Up => Move(0, -1),
			Action.Left => Move(-1, 0),
			Action.Right => Move(1, 0),
			_ => Move(0, 0)
		};

		public int GetManhattanDistanceTo(MapCoordinate coordinate)
		{
			if (coordinate == null)
			{
				throw new ArgumentNullException(nameof(coordinate));
			}

			return Math.Abs(X - coordinate.X) + Math.Abs(Y - coordinate.Y);
		}

		private MapCoordinate Move(int deltaX, int deltaY)
		{
			return new MapCoordinate(X + deltaX, Y + deltaY);
		}

		protected bool Equals(MapCoordinate other)
		{
			return X == other.X && Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((MapCoordinate)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public override string ToString()
		{
			return $"({X}, {Y})";
		}
	}
}
