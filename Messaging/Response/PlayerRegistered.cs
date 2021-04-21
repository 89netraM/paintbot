namespace PaintBot.Messaging.Response
{
	using Game.Configuration;

	public class PlayerRegistered : Response
	{
		public string GameId { get; set; }
		public string Name { get; set; }
		public GameSettings GameSettings { get; set; }
		public string GameMode { get; set; }

		public override string ToString()
		{
			return $"{Name} registered to game {GameId} for game mode {GameMode}";
		}
	}
}
