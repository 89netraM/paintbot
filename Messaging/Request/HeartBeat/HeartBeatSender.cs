namespace PaintBot.Messaging.Request.HeartBeat
{
	using System.Threading;

	public class HeartBeatSender : IHearBeatSender
	{
		private const int DefaultHeartbeatPeriodInSeconds = 30;
		private readonly IPaintBotClient _paintBotClient;

		public HeartBeatSender(IPaintBotClient paintBotClient)
		{
			_paintBotClient = paintBotClient;
		}

		public void SendHeartBeatFrom(string playerId)
		{
			new Thread(async () =>
			{
				Thread.CurrentThread.IsBackground = true;
				Thread.Sleep(DefaultHeartbeatPeriodInSeconds * 1000);
				var heartBeatRequest = new HeartBeatRequest(playerId);
				await _paintBotClient.SendAsync(heartBeatRequest, CancellationToken.None);
			}).Start();
		}
	}
}
