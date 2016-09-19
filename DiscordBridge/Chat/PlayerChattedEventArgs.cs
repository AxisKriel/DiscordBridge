using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace DiscordBridge.Chat
{
	public class PlayerChattedEventArgs : EventArgs
	{
		public Dictionary<string, Color?> ColorFormatters { get; }

		public ChatMessageBuilder Message { get; }

		public TSPlayer Player { get; }

		public PlayerChattedEventArgs(ChatMessageBuilder builder, TSPlayer player, Dictionary<string, Color?> colorDictionary = null)
		{
			Message = builder;
			Player = player;

			ColorFormatters = colorDictionary;
		}
	}
}
