namespace PaintBot
{
	using Action = Game.Action.Action;
	using Game.Configuration;
	using Game.Map;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Messaging.Response;
	using Microsoft.Xna.Framework;
	using Microsoft.Xna.Framework.Graphics;
	using Microsoft.Xna.Framework.Input;

	public class PaintBotGame : Microsoft.Xna.Framework.Game
	{
		private static readonly IEnumerable<(Color, Color)> existingColours = new[]
		{
			(Color.Blue, Color.DarkBlue),
			(Color.Green, Color.DarkGreen),
			(Color.Orange, Color.DarkOrange),
			(Color.Violet, Color.DarkViolet),
			(Color.Cyan, Color.DarkCyan),
			(Color.SeaGreen, Color.DarkSeaGreen),
			(Color.Salmon, Color.DarkSalmon),
			(Color.MediumPurple, Color.Purple),
			(Color.Goldenrod, Color.DarkGoldenrod),
		};

		private GraphicsDeviceManager graphics;
		private SpriteBatch spriteBatch;

		private Texture2D tile;
		private Texture2D tileHovering;
		private Texture2D tileSelected;
		private Texture2D player;
		private Texture2D stunned;
		private Texture2D powerUp;
		private Queue<(Color, Color)> availbleColours;
		private Dictionary<string, (Color tile, Color player)> characterColour;

		private StatePaintBot paintBot;
		private CancellationTokenSource cancellationTokenSource;
		private Task paintBotTask;

		private MouseState? lastMouseState = null;
		private MapCoordinate mouseCoordinate = null;

		private GameSettings gameSettings;
		private Map map;
		private MapUtils mapUtils;

		public PaintBotGame(StatePaintBot paintBot)
		{
			graphics = new GraphicsDeviceManager(this);
			this.paintBot = paintBot;
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
		}

		protected override void LoadContent()
		{
			spriteBatch = new SpriteBatch(GraphicsDevice);

			tile = Content.Load<Texture2D>("tile");
			tileHovering = Content.Load<Texture2D>("tileHovering");
			tileSelected = Content.Load<Texture2D>("tileSelected");
			player = Content.Load<Texture2D>("player");
			stunned = Content.Load<Texture2D>("stunned");
			powerUp = Content.Load<Texture2D>("powerUp");

			cancellationTokenSource = new CancellationTokenSource();
			paintBot.GameStartingEvent += OnGameStarting;
			paintBot.MapUpdatedEvent += OnMapUpdated;
			paintBotTask = paintBot.Run(cancellationTokenSource.Token);

			base.LoadContent();
		}

		private void OnGameStarting(GameStarting gameStarting)
		{
			gameSettings = gameStarting.GameSettings;
			characterColour = new Dictionary<string, (Color, Color)>
			{
				[gameStarting.ReceivingPlayerId] = (Color.Red, Color.DarkRed),
			};
			availbleColours = new Queue<(Color, Color)>();
			foreach (var colourPair in existingColours)
			{
				availbleColours.Enqueue(colourPair);
			}
			map = null;
			mapUtils = null;
		}

		private void OnMapUpdated(MapUpdated mapUpdated)
		{
			map = mapUpdated.Map;
			mapUtils = new MapUtils(map);
		}

		protected override void Update(GameTime gameTime)
		{
			MouseState mouseState = Mouse.GetState();

			if (map is not null && mapUtils is not null)
			{
				var (size, offset) = CalculateSizeAndOffset();
				MapCoordinate coordinate = new MapCoordinate(
					(int)Math.Round((mouseState.X - offset.x - size / 2.0f) / size),
					(int)Math.Round((mouseState.Y - offset.y - size / 2.0f) / size)
				);
				mouseCoordinate = mapUtils.IsMovementPossibleTo(coordinate) ? coordinate : null;

				if (mouseState.LeftButton == ButtonState.Released && lastMouseState?.LeftButton != ButtonState.Released)
				{
					paintBot.OverrideTarget = mouseCoordinate;
				}
			}

			lastMouseState = mouseState;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.Black);

			if (map is not null && mapUtils is not null)
			{
				spriteBatch.Begin();
				var (size, offset) = CalculateSizeAndOffset();
				int sizeI = (int)Math.Ceiling(size);

				spriteBatch.Draw(
					tile,
					new Rectangle(
						(int)Math.Floor(offset.x),
						(int)Math.Floor(offset.y),
						(int)Math.Ceiling(GraphicsDevice.Viewport.Width - offset.x * 2.0f),
						(int)Math.Ceiling(GraphicsDevice.Viewport.Height - offset.y * 2.0f)
					),
					Color.White
				);

				foreach (MapCoordinate coordinate in map.ObstaclePositions.Select(mapUtils.GetCoordinateFrom))
				{
					spriteBatch.Draw(
						tile,
						new Rectangle(
							(int)Math.Round(offset.x + coordinate.X * size),
							(int)Math.Round(offset.y + coordinate.Y * size),
							sizeI,
							sizeI
						),
						Color.Black
					);
				}
				foreach (CharacterInfo character in map.CharacterInfos)
				{
					(Color tile, Color player) colourPair;
					if (!characterColour.TryGetValue(character.Id, out colourPair))
					{
						colourPair = availbleColours.Dequeue();
						characterColour[character.Id] = colourPair;
					}
					Color colour = colourPair.tile;
					foreach (MapCoordinate coordinate in character.ColouredPositions.Select(mapUtils.GetCoordinateFrom))
					{
						spriteBatch.Draw(
							tile,
							new Rectangle(
								(int)Math.Round(offset.x + coordinate.X * size),
								(int)Math.Round(offset.y + coordinate.Y * size),
								sizeI,
								sizeI
							),
							colour
						);
					}
				}
				if (mouseCoordinate is not null)
				{
					spriteBatch.Draw(
						tileHovering,
						new Rectangle(
							(int)Math.Round(offset.x + mouseCoordinate.X * size),
							(int)Math.Round(offset.y + mouseCoordinate.Y * size),
							sizeI,
							sizeI
						),
						Color.White
					);
				}
				if (paintBot.OverrideTarget is not null)
				{
					spriteBatch.Draw(
						tileSelected,
						new Rectangle(
							(int)Math.Round(offset.x + paintBot.OverrideTarget.X * size),
							(int)Math.Round(offset.y + paintBot.OverrideTarget.Y * size),
							sizeI,
							sizeI
						),
						Color.White
					);
				}
				foreach (MapCoordinate coordinate in map.PowerUpPositions.Select(mapUtils.GetCoordinateFrom))
				{
					spriteBatch.Draw(
						powerUp,
						new Rectangle(
							(int)Math.Round(offset.x + coordinate.X * size),
							(int)Math.Round(offset.y + coordinate.Y * size),
							sizeI,
							sizeI
						),
						Color.White
					);
				}
				foreach (CharacterInfo character in map.CharacterInfos)
				{
					MapCoordinate coordinate = mapUtils.GetCoordinateFrom(character.Position);
					spriteBatch.Draw(
						player,
						new Rectangle(
							(int)Math.Round(offset.x + coordinate.X * size),
							(int)Math.Round(offset.y + coordinate.Y * size),
							sizeI,
							sizeI
						),
						characterColour[character.Id].player
					);
					if (character.CarryingPowerUp)
					{
						spriteBatch.Draw(
							powerUp,
							new Rectangle(
								(int)Math.Round(offset.x + coordinate.X * size + size / 2.0f),
								(int)Math.Round(offset.y + coordinate.Y * size + size / 2.0f),
								sizeI / 2,
								sizeI / 2
							),
							Color.White
						);
					}
					if (character.StunnedForGameTicks > 0)
					{
						spriteBatch.Draw(
							stunned,
							new Rectangle(
								(int)Math.Round(offset.x + coordinate.X * size + size / 2.0f),
								(int)Math.Round(offset.y + coordinate.Y * size + size / 2.0f),
								sizeI,
								sizeI
							),
							null,
							Color.White,
							(float)gameTime.TotalGameTime.TotalSeconds * 2.0f,
							stunned.Bounds.Size.ToVector2() / 2.0f,
							SpriteEffects.None,
							0.0f
						);
					}
				}

				spriteBatch.End();
			}

			base.Draw(gameTime);
		}

		private (float size, (float x, float y) offset) CalculateSizeAndOffset()
		{
			if (GraphicsDevice.Viewport.AspectRatio > map.Width / (float)map.Height)
			{
				float size = GraphicsDevice.Viewport.Height / (float)map.Height;
				return (
					size,
					((GraphicsDevice.Viewport.Width - map.Width * size) / 2.0f, 0.0f)
				);
			}
			else
			{
				float size = GraphicsDevice.Viewport.Width / (float)map.Width;
				return (
					size,
					(0.0f, (GraphicsDevice.Viewport.Height - map.Height * size) / 2.0f)
				);
			}
		}

		protected override void UnloadContent()
		{
			cancellationTokenSource.Cancel();
			paintBot.GameStartingEvent -= OnGameStarting;
			paintBot.MapUpdatedEvent -= OnMapUpdated;
			paintBotTask.Dispose();

			base.UnloadContent();
		}
	}
}
