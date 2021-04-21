namespace PaintBot
{
	using System.Collections.Generic;
	using System.Linq;
	using Game.Action;
	using Game.Configuration;
	using Game.Map;
	using Messaging;
	using Messaging.Request.HeartBeat;
	using Messaging.Response;
	using Serilog;

	public abstract class StatePaintBot : PaintBot
	{
		protected Map Map { get; private set; }
		protected string PlayerId { get; private set; }
		protected MapUtils MapUtils { get; private set; }

		private IEnumerator<Action> currentActionSequence;

		protected StatePaintBot(PaintBotConfig paintBotConfig, IPaintBotClient paintBotClient, IHearBeatSender heartBeatSender, ILogger logger) :
			base(paintBotConfig, paintBotClient, heartBeatSender, logger)
		{ }

		public override Action GetAction(MapUpdated mapUpdated)
		{
			Map = mapUpdated.Map;
			PlayerId = mapUpdated.ReceivingPlayerId;
			MapUtils = new MapUtils(Map);

			if (currentActionSequence is null || !currentActionSequence.MoveNext())
			{
				currentActionSequence = GetActionSequence().GetEnumerator();
				currentActionSequence.MoveNext();
			}

			return currentActionSequence.Current;
		}

		protected abstract IEnumerable<Action> GetActionSequence();
	}
}
