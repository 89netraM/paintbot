namespace PaintBot.Messaging.Request
{
	using System;

	public class RegisterMove : Request
	{
		public RegisterMove(string receivingPlayerId) : base(receivingPlayerId)
		{
		}

		public override string Type => MessageType.RegisterMove;
		public Guid GameId { get; set; }
		public int GameTick { get; set; }
		public string Direction { get; set; }
	}
}
