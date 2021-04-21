namespace PaintBot.Messaging.Response
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Game.Result;

	public class TournamentEnded : Response
	{
		public string WinningPlayerId { get; set; }
		public Guid GameId { get; set; }
		public List<PlayerPoints> GameResult { get; set; }
		public string TournamentName { get; set; }
		public string TournamentId { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append($"Tournament {TournamentName} ended. Player {GetWinningPlayerName()} won");
			sb.AppendLine();
			foreach (var result in GameResult)
				sb.Append($"{result}\n");

			return sb.ToString();
		}

		private string GetWinningPlayerName()
		{
			return GameResult.Single(g => g.PlayerId == WinningPlayerId).Name;
		}
	}
}
