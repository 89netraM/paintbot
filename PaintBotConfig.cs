namespace PaintBot
{
	using Game.Configuration;

	public class PaintBotConfig
	{
		public PaintBotConfig(string name, GameMode gameMode, int gameLengthInSeconds)
		{
			Name = name;
			GameMode = gameMode;
			GameLengthInSeconds = gameLengthInSeconds;
		}

		public GameMode GameMode { get; }
		public int GameLengthInSeconds { get; }
		public string Name { get; }
	}
}
