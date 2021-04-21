namespace PaintBot.Game.Result
{
	using System;

	public class PlayerRank
	{
		public string PlayerName { get; set; }
		public Guid PlayerId { get; set; }
		public int Rank { get; set; }
		public int Points { get; set; }
		public bool Alive { get; set; }

		public override string ToString()
			=> $"{PlayerName} ({PlayerId} - Rank: {Rank} - Points: {Points}";
	}
}
