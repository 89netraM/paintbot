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
			while (true) {
				// Implement your bot here!

				// The following is a simple example bot. It tries to
				// 1. Explode PowerUp
				// 2. Move to a tile that it is not currently owning
				// 3. Move in the direction where it can move for the longest time.

				var directions = new List<Action> { Action.Down, Action.Right, Action.Left, Action.Up };

				var myCharacter = MapUtils.GetCharacterInfoFor(PlayerId);
				var myCoordinate = MapUtils.GetCoordinateFrom(myCharacter.Position);
				var myColouredTiles = MapUtils.GetCoordinatesFrom(myCharacter.ColouredPositions);

				if (myCharacter.CarryingPowerUp)
				{
					yield return Action.Explode;
					continue;
				}

				var validActionsThatPaintsNotOwnedTile = directions.Where(dir =>
					!myColouredTiles.Contains(myCoordinate.MoveIn(dir)) && MapUtils.IsMovementPossibleTo(myCoordinate.MoveIn(dir))).ToList();

				if (validActionsThatPaintsNotOwnedTile.Any())
				{
					yield return validActionsThatPaintsNotOwnedTile.First();
					continue;
				}

				var possibleLeftMoves = 0;
				var possibleRightMoves = 0;
				var possibleUpMoves = 0;
				var possibleDownMoves = 0;

				var testCoordinate = MapUtils.GetCoordinateFrom(myCharacter.Position).MoveIn(Action.Left);
				while (MapUtils.IsMovementPossibleTo(testCoordinate))
				{
					possibleLeftMoves++;
					testCoordinate = testCoordinate.MoveIn(Action.Left);
				}

				testCoordinate = MapUtils.GetCoordinateFrom(myCharacter.Position).MoveIn(Action.Right);
				while (MapUtils.IsMovementPossibleTo(testCoordinate))
				{
					possibleRightMoves++;
					testCoordinate = testCoordinate.MoveIn(Action.Right);
				}

				testCoordinate = MapUtils.GetCoordinateFrom(myCharacter.Position).MoveIn(Action.Up);
				while (MapUtils.IsMovementPossibleTo(testCoordinate))
				{
					possibleUpMoves++;
					testCoordinate = testCoordinate.MoveIn(Action.Up);
				}

				testCoordinate = MapUtils.GetCoordinateFrom(myCharacter.Position).MoveIn(Action.Down);
				while (MapUtils.IsMovementPossibleTo(testCoordinate))
				{
					possibleDownMoves++;
					testCoordinate = testCoordinate.MoveIn(Action.Down);
				}

				var list = new List<(Action, int)>
				{
					(Action.Left, possibleLeftMoves),
					(Action.Right, possibleRightMoves),
					(Action.Up, possibleUpMoves),
					(Action.Down, possibleDownMoves)
				};

				list.Sort((first, second) => first.Item2.CompareTo(second.Item2));
				list.Reverse();

				yield return list.FirstOrDefault(l => l.Item2 > 0).Item1;
			}
		}
	}
}
