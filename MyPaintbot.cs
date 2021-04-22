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
		private readonly System.Random random;

		public MyPaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender hearBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, hearBeatSender, logger)
		{
			GameMode = paintBotConfig.GameMode;
			Name = paintBotConfig.Name ?? "My c# bot";

			random = new System.Random();
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
					}
					else
					{
						MapCoordinate closestEnemy = FindClosestEnemy();
						yield return GetDirection(closestEnemy);
					}
				}
				else
				{
					MapCoordinate closestPowerUp = FindClosestPowerUp();
					yield return GetDirection(closestPowerUp);
				}
			}
		}

		private bool ShouldExplode() =>
			Map.CharacterInfos.Any(ci =>
				ci.Id != PlayerId &&
				PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)) <= GameSettings.ExplosionRange
			);

		private MapCoordinate FindClosestEnemy() =>
			Map.CharacterInfos
				.OrderBy(ci => PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(ci.Position)))
				.FirstOrDefault(ci => ci.Id != PlayerId)?.Position
					is int p ?
						MapUtils.GetCoordinateFrom(p) :
						null;
		private MapCoordinate FindClosestPowerUp() =>
			Map.PowerUpPositions
				.OrderBy(p => PlayerCoordinate.GetManhattanDistanceTo(MapUtils.GetCoordinateFrom(p)))
				.Cast<int?>()
				.FirstOrDefault()
					is int p ?
						MapUtils.GetCoordinateFrom(p) :
						null;

		private Action GetDirection(MapCoordinate target)
		{
			if (target is not null)
			{
				IList<Action> validActions = new List<Action>();
				if (target.X < PlayerCoordinate.X && MapUtils.CanPlayerPerformAction(PlayerId, Action.Left))
				{
					validActions.Add(Action.Left);
				}
				else if (target.X > PlayerCoordinate.X && MapUtils.CanPlayerPerformAction(PlayerId, Action.Right))
				{
					validActions.Add(Action.Right);
				}
				if (target.Y < PlayerCoordinate.Y && MapUtils.CanPlayerPerformAction(PlayerId, Action.Up))
				{
					validActions.Add(Action.Up);
				}
				else if (target.Y > PlayerCoordinate.Y && MapUtils.CanPlayerPerformAction(PlayerId, Action.Down))
				{
					validActions.Add(Action.Down);
				}
				IList<Action> preferredActions = validActions.Where(a => !PlayerColouredCoordinates.Contains(PlayerCoordinate.MoveIn(a))).ToList();

				if (preferredActions.Count > 0)
				{
					return preferredActions[random.Next(preferredActions.Count)];
				}
				else if (validActions.Count > 0)
				{
					return validActions[random.Next(validActions.Count)];
				}
				else
				{
					return Action.Stay;
				}
			}
			else
			{
				return Action.Stay;
			}
		}
	}
}
