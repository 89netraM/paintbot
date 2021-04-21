namespace PaintBot.Messaging.Response
{
	using System;
	using System.Text;
	using Game.Result;

	public class GameResult : Response
	{
		public Guid GameId { get; set; }
		public PlayerRank[] PlayerRanks { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Game result is in for game: {GameId}");
			foreach (var rank in PlayerRanks)
				sb.Append($"{rank}\n");

			return sb.ToString();
		}

	}
}
