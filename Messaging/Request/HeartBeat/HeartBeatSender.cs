namespace PaintBot.Messaging.Request.HeartBeat
{
	using System.Threading;
	using System.Threading.Tasks;

	public class HeartBeatSender : IHearBeatSender
	{
		private const int DefaultHeartbeatPeriodInSeconds = 30;
		private readonly IPaintBotClient _paintBotClient;

		public HeartBeatSender(IPaintBotClient paintBotClient)
		{
			_paintBotClient = paintBotClient;
		}

		public void SendHeartBeatFrom(string playerId, CancellationToken ct)
		{
			new Thread(async () =>
			{
				Thread.CurrentThread.IsBackground = true;
				try
				{
					await Task.Delay(DefaultHeartbeatPeriodInSeconds * 1000, ct);
				}
				catch (TaskCanceledException) { }
				if (!ct.IsCancellationRequested && _paintBotClient.IsOpen)
				{
					var heartBeatRequest = new HeartBeatRequest(playerId);
					await _paintBotClient.SendAsync(heartBeatRequest, CancellationToken.None);
				}
			}).Start();
		}
	}
}
