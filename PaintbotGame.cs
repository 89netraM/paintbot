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
		private Texture2D player;
		private Texture2D stunned;
		private Texture2D powerUp;
		private Queue<(Color, Color)> availbleColours;
		private Dictionary<string, (Color tile, Color player)> characterColour;

		private PaintBot paintBot;
		private CancellationTokenSource cancellationTokenSource;
		private Task paintBotTask;

		private KeyboardState? lastKeyboardState = null;
		private GamePadState? lastGamePadState = null;
		private Action? mostRecentAction = null;
		private Action? MostRecentAction
		{
			get => mostRecentAction;
			set
			{
				if (mostRecentAction != Action.Explode || value is null)
				{
					mostRecentAction = value;
				}
			}
		}

		private GameSettings gameSettings;
		private Map map;
		private MapUtils mapUtils;
		private bool reRender = false;

		public PaintBotGame(PaintBot paintBot)
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
			player = Content.Load<Texture2D>("player");
			stunned = Content.Load<Texture2D>("stunned");
			powerUp = Content.Load<Texture2D>("powerUp");

			cancellationTokenSource = new CancellationTokenSource();
			paintBot.GameStartingEvent += OnGameStarting;
			paintBot.MapUpdatedEvent = OnMapUpdated;
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

		private async Task<Action> OnMapUpdated(MapUpdated mapUpdated, CancellationToken ct)
		{
			map = mapUpdated.Map;
			mapUtils = new MapUtils(map);

			await Task.Delay(Math.Max((int)Math.Round(gameSettings.TimeInMsPerTick * 0.9f), 100), ct);
			Action returnAction = mostRecentAction ?? Action.Stay;
			mostRecentAction = null;
			return returnAction;
		}

		protected override void Update(GameTime gameTime)
		{
			KeyboardState keyboardState = Keyboard.GetState();
			GamePadState gamePadState = GamePad.GetState(0);

			if ((keyboardState.IsKeyDown(Keys.Space) && lastKeyboardState?.IsKeyDown(Keys.Space) != true) ||
				(gamePadState.IsButtonDown(Buttons.A) && lastGamePadState?.IsButtonDown(Buttons.A) != true))
			{
				MostRecentAction = Action.Explode;
			}
			else if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left) ||
				gamePadState.IsButtonDown(Buttons.DPadLeft) || gamePadState.IsButtonDown(Buttons.LeftThumbstickLeft))
			{
				MostRecentAction = Action.Left;
			}
			else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right) ||
				gamePadState.IsButtonDown(Buttons.DPadRight) || gamePadState.IsButtonDown(Buttons.LeftThumbstickRight))
			{
				MostRecentAction = Action.Right;
			}
			else if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up) ||
				gamePadState.IsButtonDown(Buttons.DPadUp) || gamePadState.IsButtonDown(Buttons.LeftThumbstickUp))
			{
				MostRecentAction = Action.Up;
			}
			else if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down) ||
				gamePadState.IsButtonDown(Buttons.DPadDown) || gamePadState.IsButtonDown(Buttons.LeftThumbstickDown))
			{
				MostRecentAction = Action.Down;
			}
			else
			{
				MostRecentAction = Action.Stay;
			}

			lastKeyboardState = keyboardState;
			lastGamePadState = gamePadState;

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
			paintBot.MapUpdatedEvent = null;
			paintBotTask.Dispose();

			base.UnloadContent();
		}
	}
}
