namespace PaintBot.Messaging
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net.WebSockets;
	using System.Runtime.CompilerServices;
	using System.Text.Json;
	using System.Threading;
	using System.Threading.Tasks;
	using Game.Configuration;
	using Response;
	using static Serilog.Log;

	public class PaintBotClient : IPaintBotClient
	{
		private readonly ClientWebSocket _clientWebSocket;
		private readonly PaintBotServerConfig _config;
		private readonly JsonSerializerOptions _serializeOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters =
			{
				new ResponseConverter()
			} // TODO: Should we move this out of the client to make the it less aware of the messages?
		};

		public bool IsOpen { get; private set; } = false;

		public PaintBotClient(PaintBotServerConfig config)
		{
			_config = config;
			_clientWebSocket = new ClientWebSocket();
		}

		public async Task ConnectAsync(GameMode gameMode, CancellationToken ct)
		{
			var uri = new Uri($"{_config.BaseUrl}/{gameMode.ToString().ToLower()}");
			await _clientWebSocket.ConnectAsync(uri, ct);
			IsOpen = true;
		}

		public async Task SendAsync<T>(T message, CancellationToken ct)
		{
			var data = JsonSerializer.SerializeToUtf8Bytes(message, typeof(T), _serializeOptions);
			await _clientWebSocket.SendAsync(data, WebSocketMessageType.Text, true, ct);
		}

		public async Task<T> ReceiveAsync<T>(CancellationToken ct) where T : class
		{
			try
			{
				var buffer = new ArraySegment<byte>(new byte[1024]);
				using (var ms = new MemoryStream())
				{
					WebSocketReceiveResult result;
					do
					{
						result = await _clientWebSocket.ReceiveAsync(buffer, ct);
						ms.Write(buffer.Array ?? throw new InvalidOperationException(), buffer.Offset, result.Count);
					} while (!result.EndOfMessage);

					if (result.MessageType == WebSocketMessageType.Close)
						return default; // TODO: Handle reconnection?

					ms.Position = 0;

					return await JsonSerializer.DeserializeAsync<T>(ms, _serializeOptions, ct);
				}

			}
			catch (Exception ex)
			{
				Logger.Error(ex, ex.Message);
			}

			await Task.CompletedTask;

			return null;
		}

		public async IAsyncEnumerable<T> ReceiveEnumerableAsync<T>([EnumeratorCancellation] CancellationToken ct) where T : class
		{
			while (true)
				yield return await ReceiveAsync<T>(ct);
		}

		public void Close()
		{
			_clientWebSocket.Dispose(); // What do we think about disposing this here?
			IsOpen = false;
		}
	}
}
