namespace PaintBot.Messaging.Request.HeartBeat
{
	using System.Threading;

	public interface IHearBeatSender
	{
		void SendHeartBeatFrom(string playerId, CancellationToken ct);
	}
}
