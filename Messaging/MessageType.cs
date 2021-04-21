namespace PaintBot.Messaging
{
	public class MessageType
	{
		public const string ClientInfo = "se.cygni.paintbot.api.request.ClientInfo";
		public const string StartGame = "se.cygni.paintbot.api.request.StartGame";
		public const string RegisterMove = "se.cygni.paintbot.api.request.RegisterMove";
		public const string RegisterPlayer = "se.cygni.paintbot.api.request.RegisterPlayer";
		public const string HeartBeatRequest = "se.cygni.paintbot.api.request.HeartBeatRequest";

		// Responses
		public const string HeartBeatResponse = "se.cygni.paintbot.api.response.HeartBeatResponse";
		public const string PlayerRegistered = "se.cygni.paintbot.api.response.PlayerRegistered";

		// Exceptions
		public const string InvalidMessage = "";
		public const string InvalidPlayerName = "se.cygni.paintbot.api.exception.InvalidPlayerName";
		public const string NoActiveTournament = "";

		// Events
		public const string ArenaUpdate = "";
		public const string CharacterStunned = "se.cygni.paintbot.api.event.CharactedStunnedEvent";
		public const string GameAborted = "";
		public const string GameChanged = "";
		public const string GameCreated = "";
		public const string GameEnded = "se.cygni.paintbot.api.event.GameEndedEvent";
		public const string GameLink = "se.cygni.paintbot.api.event.GameLinkEvent";
		public const string GameResult = "se.cygni.paintbot.api.event.GameResultEvent";
		public const string GameStarting = "se.cygni.paintbot.api.event.GameStartingEvent";
		public const string MapUpdate = "se.cygni.paintbot.api.event.MapUpdateEvent";
		public const string TournamentEnded = "se.cygni.paintbot.api.event.TournamentEndedEvent";
	}
}
