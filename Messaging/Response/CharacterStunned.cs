namespace PaintBot.Messaging.Response
{
	using System;
	using Game.Action;

	public class CharacterStunned : Response
	{
		public StunReason StunReason { get; set; }
		public int DurationInTicks { get; set; }
		public string PlayerId { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public Guid GameId { get; set; }
		public int GameTick { get; set; }

		public override string ToString()
		{
			return $"Player {PlayerId} in game {GameId} stunned for {DurationInTicks} at tick {GameTick} position ({X}, {Y}). \n" +
				   $"Reason: {StunReason}";
		}
	}
}
