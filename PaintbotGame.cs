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
		private Texture2D cross;
		private Texture2D player;
		private Texture2D stunned;
		private Texture2D powerUp;
		private SpriteFont cascadiaMono;
		private Queue<(Color, Color)> availbleColours;
		private Dictionary<string, (Color tile, Color player)> characterColour;
		private int uiWidth = 0;

		private StatePaintBot paintBot;
		private CancellationTokenSource cancellationTokenSource;
		private Task paintBotTask;

		private MouseState? lastMouseState = null;
		private KeyboardState? lastKeyboardState = null;
		private MapCoordinate mouseCoordinate = null;

		private GameSettings gameSettings;
		private Map map;
		private MapUtils mapUtils;
		private long tickTime = 0;
		private long topTickTime = 0;

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
			cross = Content.Load<Texture2D>("cross");
			player = Content.Load<Texture2D>("player");
			stunned = Content.Load<Texture2D>("stunned");
			powerUp = Content.Load<Texture2D>("powerUp");
			cascadiaMono = Content.Load<SpriteFont>("cascadiaMono");

			cancellationTokenSource = new CancellationTokenSource();
			paintBot.GameStartingEvent += OnGameStarting;
			paintBot.MapUpdatedEvent += OnMapUpdated;
			paintBot.TimingEvent += OnTime;
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

		private void OnTime(long time)
		{
			tickTime = time;
			topTickTime = Math.Max(tickTime, topTickTime);
		}

		protected override void Update(GameTime gameTime)
		{
			MouseState mouseState = Mouse.GetState();
			KeyboardState keyboardState = Keyboard.GetState();

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
					if (paintBot.OverrideTarget?.Equals(mouseCoordinate) == true)
					{
						paintBot.OverrideTarget = null;
					}
					else
					{
						paintBot.OverrideTarget = mouseCoordinate;
					}
				}

				if (mouseCoordinate is not null &&
					mouseState.MiddleButton == ButtonState.Released &&
					lastMouseState?.MiddleButton != ButtonState.Released)
				{
					if (paintBot.Disallowed.Contains(mouseCoordinate))
					{
						paintBot.Disallowed.Remove(mouseCoordinate);
					}
					else
					{
						paintBot.Disallowed.Add(mouseCoordinate);
					}
				}

				if (keyboardState.IsKeyDown(Keys.Space) && lastKeyboardState?.IsKeyDown(Keys.Space) != true)
				{
					paintBot.ForceExplode = true;
				}

				if (keyboardState.IsKeyDown(Keys.Escape) && lastKeyboardState?.IsKeyDown(Keys.Escape) != true)
				{
					paintBot.Disallowed.Clear();
				}
			}

			lastMouseState = mouseState;
			lastKeyboardState = keyboardState;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.Black);

			if (map is not null && mapUtils is not null)
			{
				foreach (CharacterInfo character in map.CharacterInfos)
				{
					if (!characterColour.ContainsKey(character.Id))
					{
						characterColour[character.Id] = availbleColours.Dequeue();
					}
				}

				spriteBatch.Begin();
				DrawUI(gameTime);
				DrawArena(gameTime);
				spriteBatch.End();
			}

			base.Draw(gameTime);
		}

		private void DrawUI(GameTime gameTime)
		{
			const string timeLabel = "Time:";
			int timeSeconds = (int)Math.Round(gameSettings.GameDurationInSeconds - map.WorldTick * gameSettings.TimeInMsPerTick / 1000.0f);
			string timeString = $"{timeSeconds / 60:d2}:{timeSeconds % 60:d2}";
			float timeStringWidth = cascadiaMono.MeasureString(timeString).X;
			const string timingLabel = "Tick time:";
			string timingString = $"{tickTime} ms";
			float timingStringWidth = cascadiaMono.MeasureString(timingString).X;
			const string topTimingLabel = "Top tick time:";
			string topTimingString = $"{topTickTime} ms";
			float topTimingStringWidth = cascadiaMono.MeasureString(topTimingString).X;

			uiWidth = (int)Math.Ceiling(new[] {
				cascadiaMono.MeasureString(timeLabel).X,
				timeStringWidth,
				cascadiaMono.MeasureString(timingLabel).X,
				timingStringWidth,
				cascadiaMono.MeasureString(topTimingLabel).X,
				topTimingStringWidth
			}.Max());
			IDictionary<string, float> characterPointsWidth = new Dictionary<string, float>();
			foreach (CharacterInfo ci in map.CharacterInfos)
			{
				ci.Name = ci.Name.Substring(0, Math.Min(ci.Name.Length, 20));
				uiWidth = Math.Max((int)Math.Ceiling(cascadiaMono.MeasureString($"{ci.Name}:").X), uiWidth);
				Vector2 pointsSize = cascadiaMono.MeasureString(ci.Points.ToString());
				uiWidth = Math.Max((int)Math.Ceiling(pointsSize.X), uiWidth);
				characterPointsWidth[ci.Id] = pointsSize.X;
			}
			const int uiSpacing = 5;
			uiWidth += uiSpacing * 2;

			int uiTop = uiSpacing;
			int uiCardHeight = cascadiaMono.LineSpacing * 2 + uiSpacing * 2;
			foreach (CharacterInfo ci in map.CharacterInfos.OrderByDescending(ci => ci.Points))
			{
				spriteBatch.Draw(
					tile,
					new Rectangle(
						uiSpacing,
						uiTop,
						uiWidth,
						uiCardHeight
					),
					characterColour[ci.Id].tile
				);
				spriteBatch.DrawString(
					cascadiaMono,
					$"{ci.Name}:",
					new Vector2(
						uiSpacing * 2,
						uiTop + uiSpacing
					),
					Color.White
				);
				spriteBatch.DrawString(
					cascadiaMono,
					ci.Points.ToString(),
					new Vector2(
						uiWidth - characterPointsWidth[ci.Id],
						uiTop + uiSpacing + cascadiaMono.LineSpacing
					),
					Color.White
				);
				uiTop += uiCardHeight + uiSpacing;
			}

			uiWidth += uiSpacing * 2;

			spriteBatch.DrawString(
				cascadiaMono,
				timeLabel,
				new Vector2(
					uiSpacing * 2,
					GraphicsDevice.Viewport.Height - (cascadiaMono.LineSpacing * 6 + uiSpacing)
				),
				Color.White
			);
			spriteBatch.DrawString(
				cascadiaMono,
				timeString,
				new Vector2(
					uiWidth - uiSpacing * 2 - timeStringWidth,
					GraphicsDevice.Viewport.Height - (cascadiaMono.LineSpacing * 5 + uiSpacing)
				),
				Color.White
			);
			spriteBatch.DrawString(
				cascadiaMono,
				timingLabel,
				new Vector2(
					uiSpacing * 2,
					GraphicsDevice.Viewport.Height - (cascadiaMono.LineSpacing * 4 + uiSpacing)
				),
				Color.White
			);
			spriteBatch.DrawString(
				cascadiaMono,
				timingString,
				new Vector2(
					uiWidth - uiSpacing * 2 - timingStringWidth,
					GraphicsDevice.Viewport.Height - (cascadiaMono.LineSpacing * 3 + uiSpacing)
				),
				Color.White
			);
			spriteBatch.DrawString(
				cascadiaMono,
				topTimingLabel,
				new Vector2(
					uiSpacing * 2,
					GraphicsDevice.Viewport.Height - (cascadiaMono.LineSpacing * 2 + uiSpacing)
				),
				Color.White
			);
			spriteBatch.DrawString(
				cascadiaMono,
				topTimingString,
				new Vector2(
					uiWidth - uiSpacing * 2 - topTimingStringWidth,
					GraphicsDevice.Viewport.Height - (cascadiaMono.LineSpacing * 1 + uiSpacing)
				),
				Color.White
			);
		}

		private void DrawArena(GameTime gameTime)
		{
			var (size, offset) = CalculateSizeAndOffset();
			int sizeI = (int)Math.Ceiling(size);

			spriteBatch.Draw(
				tile,
				new Rectangle(
					(int)Math.Floor(offset.x),
					(int)Math.Floor(offset.y),
					(int)Math.Ceiling(GraphicsDevice.Viewport.Width + uiWidth - offset.x * 2.0f),
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
						characterColour[character.Id].tile
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
			foreach (MapCoordinate coordinate in paintBot.Disallowed)
			{
				spriteBatch.Draw(
					cross,
					new Rectangle(
						(int)Math.Round(offset.x + coordinate.X * size),
						(int)Math.Round(offset.y + coordinate.Y * size),
						sizeI,
						sizeI
					),
					Color.Red
				);
			}
		}

		private (float size, (float x, float y) offset) CalculateSizeAndOffset()
		{
			float availbleWidth = GraphicsDevice.Viewport.Width - uiWidth;
			if (availbleWidth / (float)GraphicsDevice.Viewport.Height > map.Width / (float)map.Height)
			{
				float size = GraphicsDevice.Viewport.Height / (float)map.Height;
				return (
					size,
					(uiWidth + (availbleWidth - map.Width * size) / 2.0f, 0.0f)
				);
			}
			else
			{
				float size = availbleWidth / (float)map.Width;
				return (
					size,
					((float)uiWidth, (GraphicsDevice.Viewport.Height - map.Height * size) / 2.0f)
				);
			}
		}

		protected override void UnloadContent()
		{
			cancellationTokenSource.Cancel();
			paintBot.GameStartingEvent -= OnGameStarting;
			paintBot.MapUpdatedEvent -= OnMapUpdated;
			paintBot.TimingEvent -= OnTime;
			paintBotTask.Dispose();

			base.UnloadContent();
		}
	}
}
