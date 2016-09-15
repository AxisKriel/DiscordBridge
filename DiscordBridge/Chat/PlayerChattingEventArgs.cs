using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace DiscordBridge.Chat
{
	public class PlayerChattingEventArgs : EventArgs
	{
		public ChatMessageBuilder Message { get; set; }

		public TSPlayer Player { get; set; }

		public string RawText { get; }

		public PlayerChattingEventArgs(string text)
		{
			Message = new ChatMessageBuilder().SetFormat("{4}").SetText(text);
			RawText = text;
		}

		public PlayerChattingEventArgs(ChatMessageBuilder builder, TSPlayer player, string rawText)
		{
			Message = builder;
			Player = player;
			RawText = rawText;
		}
	}
}
