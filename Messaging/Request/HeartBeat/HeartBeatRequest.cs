namespace PaintBot.Messaging.Request.HeartBeat
{
	public class HeartBeatRequest : Request
	{
		public HeartBeatRequest(string receivingPlayerId) : base(receivingPlayerId)
		{
		}

		public override string Type => MessageType.HeartBeatRequest;
	}
}
