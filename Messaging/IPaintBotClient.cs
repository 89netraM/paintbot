namespace PaintBot.Messaging
{
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Game.Configuration;

	public interface IPaintBotClient
	{
		bool IsOpen { get; }
		Task ConnectAsync(GameMode gameMode, CancellationToken ct);
		Task SendAsync<T>(T message, CancellationToken ct);
		Task<T> ReceiveAsync<T>(CancellationToken ct) where T : class;
		IAsyncEnumerable<T> ReceiveEnumerableAsync<T>(CancellationToken ct) where T : class;
		void Close();
	}
}
