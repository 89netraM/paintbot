namespace PaintBot.Messaging.Response
{
	using System;
	using Game.Map;

	public class MapUpdated : Response
	{
		public int GameTick { get; set; }
		public Guid GameId { get; set; }
		public Map Map { get; set; }

		public override string ToString() => $"Tick {GameTick}";
	}
}
