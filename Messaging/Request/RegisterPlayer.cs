namespace PaintBot.Messaging.Request
{
	using Game.Configuration;

	public class RegisterPlayer : Request
	{
		public RegisterPlayer(string playerName, GameSettings gameSettings) : base("")
		{
			PlayerName = playerName;
			GameSettings = gameSettings;
		}

		public override string Type => MessageType.RegisterPlayer;
		public string PlayerName { get; }
		public GameSettings GameSettings { get; }
	}
}
