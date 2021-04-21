namespace PaintBot.Messaging.Response
{
	public class HeartBeatResponse : Response
	{
		public override string ToString()
		{
			return "Received heartbeat from server";
		}
	}
}
