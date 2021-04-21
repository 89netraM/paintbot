namespace PaintBot.Messaging.Response
{
	using System;
	using Game.Map;

	public class GameEnded : Response
	{
		public Guid PlayerWinnerId { get; set; }
		public string PlayerWinnerName { get; set; }
		public Guid GameId { get; set; }
		public int GameTick { get; set; }
		public Map Map { get; set; }

		public override string ToString()
		{
			return $"Game ended. {PlayerWinnerName} won!";
		}
	}
}
