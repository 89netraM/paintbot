namespace PaintBot.Messaging.Response
{
	public class InvalidPlayerName : Response // Not implemented
	{
		public enum PlayerNameInvalidReason
		{
			Taken,
			Empty,
			InvalidCharacter
		}

		public PlayerNameInvalidReason ReasonCode { get; set; }

		public override string ToString()
		{
			return $"Player name was invalid. Reason: {ReasonCode}";
		}
	}
}
