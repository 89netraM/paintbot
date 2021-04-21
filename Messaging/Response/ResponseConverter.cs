namespace PaintBot.Messaging.Response
{
	using System;
	using System.Text.Json;
	using System.Text.Json.Serialization;

	public class ResponseConverter : JsonConverter<Response>
	{
		public override Response Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
		{
			var doc = JsonDocument.ParseValue(ref reader);
			var json = doc.RootElement.GetRawText();
			var messageType = doc.RootElement.GetProperty("type").GetString();

			return messageType switch
			{
				MessageType.PlayerRegistered => JsonSerializer.Deserialize<PlayerRegistered>(json, options),
				MessageType.InvalidPlayerName => JsonSerializer.Deserialize<InvalidPlayerName>(json, options),
				MessageType.MapUpdate => JsonSerializer.Deserialize<MapUpdated>(json, options),
				MessageType.GameResult => JsonSerializer.Deserialize<GameResult>(json, options),
				MessageType.GameLink => JsonSerializer.Deserialize<GameLink>(json, options),
				MessageType.GameEnded => JsonSerializer.Deserialize<GameEnded>(json, options),
				MessageType.GameStarting => JsonSerializer.Deserialize<GameStarting>(json, options),
				MessageType.CharacterStunned => JsonSerializer.Deserialize<CharacterStunned>(json, options),
				MessageType.TournamentEnded => JsonSerializer.Deserialize<TournamentEnded>(json, options),
				MessageType.HeartBeatResponse => JsonSerializer.Deserialize<HeartBeatResponse>(json, options),
				_ => null
			};
		}

		public override void Write(Utf8JsonWriter writer, Response value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
	}
}
