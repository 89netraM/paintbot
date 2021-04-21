namespace PaintBot.Game.Map
{
	public class Map
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public int WorldTick { get; set; }
		public CharacterInfo[] CharacterInfos { get; set; }
		public int[] PowerUpPositions { get; set; }
		public int[] ObstaclePositions { get; set; }
		public CollisionInfo[] CollisionInfos { get; set; }
		public ExplosionInfo[] ExplosionInfos { get; set; }
	}
}
