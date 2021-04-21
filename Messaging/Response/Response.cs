namespace PaintBot.Messaging.Response
{
	public abstract class Response
	{
		public string Type { get; set; }
		public string ReceivingPlayerId { get; set; }
		public long Timestamp { get; set; }
	}
}
