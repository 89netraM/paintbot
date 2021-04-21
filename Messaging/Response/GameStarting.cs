namespace PaintBot.Messaging.Response
{
	using System;
	using Game.Configuration;

	public class GameStarting : Response
	{
		public Guid GameId { get; set; }
		public int NoofPlayers { get; set; }
		public int Width { get; set; }
		public int GameHeight { get; set; }
		public GameSettings GameSettings { get; set; }

		public override string ToString()
		{
			return $"Game {GameId} starting with {NoofPlayers} players";
		}
	}
}
