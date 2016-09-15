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
		public ChatMessageBuilder Message { get; }

		public TSPlayer Player { get; }

		public PlayerChattedEventArgs(ChatMessageBuilder builder, TSPlayer player)
		{
			Message = builder;
			Player = player;
		}
	}
}
