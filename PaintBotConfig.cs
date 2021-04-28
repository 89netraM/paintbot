namespace PaintBot
{
	using Game.Configuration;

	public class PaintBotConfig
	{
		public PaintBotConfig(string name, GameMode gameMode, int gameLengthInSeconds, VisualMode visualMode)
		{
			Name = name;
			GameMode = gameMode;
			GameLengthInSeconds = gameLengthInSeconds;
			VisualMode = visualMode;
		}

		public GameMode GameMode { get; }
		public int GameLengthInSeconds { get; }
		public string Name { get; }
		public VisualMode VisualMode { get; }
	}
}
