namespace PaintBot.Game.Map
{
	public class CharacterInfo
	{
		public string Name { get; set; }
		public int Points { get; set; }
		public int Position { get; set; }
		public int[] ColouredPositions { get; set; }
		public int StunnedForGameTicks { get; set; }
		public string Id { get; set; }
		public bool CarryingPowerUp { get; set; }
	}
}
