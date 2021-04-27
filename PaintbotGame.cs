namespace PaintBot
{
	using Microsoft.Xna.Framework;
	using Microsoft.Xna.Framework.Graphics;
	using Microsoft.Xna.Framework.Input;

	public class PaintBotGame : Microsoft.Xna.Framework.Game
	{
		private GraphicsDeviceManager graphics;
		private SpriteBatch spriteBatch;

		public PaintBotGame()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
		}

		protected override void LoadContent()
		{
			spriteBatch = new SpriteBatch(GraphicsDevice);

			base.LoadContent();
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.White);

			base.Draw(gameTime);
		}
	}
}
