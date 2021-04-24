namespace PaintBot
{
	using Game.Configuration;

	public class PaintBotConfig
	{
		public PaintBotConfig(string name, GameMode gameMode, int gameLengthInSeconds, bool shouldWriteMap)
		{
			Name = name;
			GameMode = gameMode;
			GameLengthInSeconds = gameLengthInSeconds;
			ShouldWriteMap = shouldWriteMap;
		}

		public GameMode GameMode { get; }
		public int GameLengthInSeconds { get; }
		public string Name { get; }
		public bool ShouldWriteMap { get; }
	}
}
