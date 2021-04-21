namespace PaintBot.Messaging.Request.HeartBeat
{
	public interface IHearBeatSender
	{
		void SendHeartBeatFrom(string playerId);
	}
}
