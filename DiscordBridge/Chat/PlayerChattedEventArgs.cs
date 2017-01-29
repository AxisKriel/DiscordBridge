using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace DiscordBridge.Chat
{
	public class PlayerChattedEventArgs : EventArgs
	{
		public Dictionary<string, string> ChatFormatters { get; }

		public Dictionary<string, Color?> ColorFormatters { get; }

		public ChatMessageBuilder Message { get; }

		public TSPlayer Player { get; }

		public PlayerChattedEventArgs(PlayerChattingEventArgs args)
		{
			Message = args.Message;
			Player = args.Player;

			ChatFormatters = args.ChatFormatters;
			ColorFormatters = args.ColorFormatters;
		}

		public PlayerChattedEventArgs(ChatMessageBuilder builder, TSPlayer player, Dictionary<string, string> chatDictionary, Dictionary<string, Color?> colorDictionary)
		{
			Message = builder;
			Player = player;

			ChatFormatters = chatDictionary;
			ColorFormatters = colorDictionary;
		}
	}
}
