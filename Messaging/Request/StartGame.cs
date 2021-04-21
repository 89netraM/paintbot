namespace PaintBot.Messaging.Request
{
	public class StartGame : Request
	{
		public StartGame(string receivingPlayerId) : base(receivingPlayerId)
		{
		}

		public override string Type => MessageType.StartGame;
	}
}
