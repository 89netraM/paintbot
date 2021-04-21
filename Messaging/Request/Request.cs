namespace PaintBot.Messaging.Request
{
	using System;

	public abstract class Request
	{
		protected Request(string receivingReceivingPlayerId)
		{
			var couldParsePlayerId = Guid.TryParse(receivingReceivingPlayerId, out var id);
			if (couldParsePlayerId)
				ReceivingPlayerId = id;
			else
				ReceivingPlayerId = Guid.Empty;
		}

		public abstract string Type { get; }
		public Guid? ReceivingPlayerId { get; } // Move to base class?
		public long Timestamp { get; } = DateTimeOffset.Now.ToUnixTimeSeconds();
	}
}
