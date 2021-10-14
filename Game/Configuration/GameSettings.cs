namespace PaintBot.Game.Configuration
{
	public class GameSettings
	{
		public int MaxNoofPlayers { get; set; } = 5;
		public int TimeInMsPerTick { get; set; } = 250;
		public bool ObstaclesEnabled { get; set; } = true;
		public bool PowerUpsEnabled { get; set; } = true;
		public int AddPowerUpLikelihood { get; set; } = 15;
		public int RemovePowerUpLikelihood { get; set; } = 5;
		public bool TrainingGame { get; set; } = true;
		public int PointsPerTileOwned { get; set; } = 1;
		public int PointsPerCausedStun { get; set; } = 5;
		public int NoOfTicksInvulnerableAfterStun { get; set; } = 3;
		public int MinNoOfTicksStunned { get; set; } = 8;
		public int MaxNoOfTicksStunned { get; set; } = 10;
		public int StartObstacles { get; set; } = 30;
		public int StartPowerUps { get; set; } = 1;
		public int GameDurationInSeconds { get; set; } = 180;
		public int ExplosionRange { get; set; } = 4;
		public bool PointsPerTick { get; set; } = false;
	}
}
