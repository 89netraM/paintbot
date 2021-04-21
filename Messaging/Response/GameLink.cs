namespace PaintBot.Messaging.Response
{
	public class GameLink : Response
	{
		public string GameId { get; set; }
		public string Url { get; set; }

		public override string ToString()
		{
			return $"Game link : {Url}";
		}
	}
}
