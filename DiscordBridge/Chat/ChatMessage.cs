using System.Collections.Generic;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace DiscordBridge.Chat
{
	public class ChatMessage
	{
		public struct Section
		{
			public Color? Color { get; set; }

			public string Text { get; set; }

			public Section(string text)
			{
				Color = null;
				Text = text;
			}

			public Section(string text, Color? color)
			{
				Color = color;
				Text = text;
			}

			public override string ToString()
			{
				if (!Color.HasValue)
					return Text;
				else
					return TShock.Utils.ColorTag(Text, Color.Value);
			}
		}

		public Color? Color { get; set; }

		public Section Header { get; set; }

		public Section Name { get; set; }

		public List<Section> Prefixes { get; set; } = new List<Section>();

		public string PrefixSeparator { get; set; } = " ";

		public List<Section> Suffixes { get; set; } = new List<Section>();

		public string SuffixSeparator { get; set; } = " ";

		public string Text { get; set; }
	}
}
