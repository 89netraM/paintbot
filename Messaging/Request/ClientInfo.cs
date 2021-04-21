namespace PaintBot.Messaging.Request
{
	public class ClientInfo : Request
	{
		public ClientInfo(string language, string languageVersion, string operatingSystem, string operatingSystemVersion, string clientVersion, string receivingPlayerId) : base(receivingPlayerId)
		{
			Language = language;
			LanguageVersion = languageVersion;
			OperatingSystem = operatingSystem;
			OperatingSystemVersion = operatingSystemVersion;
			ClientVersion = clientVersion;
		}

		public override string Type => MessageType.ClientInfo;
		public string Language { get; }
		public string LanguageVersion { get; }
		public string OperatingSystem { get; }
		public string OperatingSystemVersion { get; }
		public string ClientVersion { get; }
	}
}
