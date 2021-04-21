namespace PaintBot.Game.Result
{
	public class PlayerPoints
	{
		public string Name { get; set; }
		public string PlayerId { get; set; }
		public int Points { get; set; }

		public override string ToString()
		 => $"{Name} (${PlayerId}) - {Points} points";
	}
}
