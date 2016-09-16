using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria;
using TShockAPI;

namespace DiscordBridge.Extensions
{
	public static class StringExtensions
	{
		private class Tag
		{
			enum Type
			{
				None,
				Color,
				Item,
				Name,
				Achievement,
				Glyph
			}

			private string _raw { get; }

			private Type _type { get; }

			public string Options { get; }

			public string Text { get; }

			public Tag(Match match) : this(match.Groups["tag"].Value, match.Groups["options"].Value, match.Groups["text"].Value)
			{
				_raw = match.Value;
			}

			private Tag(string type, string options, string text)
			{
				switch (type)
				{
					case "c":
					case "color":
						_type = Type.Color;
						break;

					case "i":
					case "item":
						_type = Type.Item;
						break;

					case "n":
					case "name":
						_type = Type.Name;
						break;

					case "a":
					case "achievement":
						_type = Type.Achievement;
						break;

					case "g":
					case "glyph":
						_type = Type.Glyph;
						break;

					default:
						_type = Type.None;
						break;
				}

				Options = options;
				Text = text;
			}

			public string Parse(bool quotes = false)
			{
				switch (_type)
				{
					case Type.None:
					case Type.Color:
					case Type.Name:
					// Sadly there is no way left in TSAPI for getting an achievement name, so we have to keep it as the tag text
					case Type.Achievement:
					case Type.Glyph:
					default:
						return Text;

					case Type.Item:
						Item item = TShock.Utils.GetItemFromTag(_raw);
						string stack = item.stack > 1 ? $"{item.stack} " : "";
						if (quotes)
							return $"`{stack}{item.AffixName()}`";
						else
							return $"{stack}{item.AffixName()}";
				}
			}

			public override string ToString() => _raw;
		}

		/// <summary>
		/// Strips any Terraria chat tag from the given string.
		/// Color and glyph tags are turned into normal text.
		/// Achievement, Player and Item tags are changed to their respective names.
		/// </summary>
		/// <param name="s">The string to strip tags from (normally a Terraria chat message).</param>
		/// <param name="quoteResult">
		/// Whether or not to quote the resulting text with backticks (`), if any.
		/// Useful if the message is being displayed in a client that supports markdown.
		/// </param>
		/// <returns>A string with no chat tags.</returns>
		public static string StripTags(this string s, bool quoteResult = false)
		{
			// Source: Terraria
			var regex = new Regex("(?<!\\\\)\\[(?<tag>[a-zA-Z]{1,10})(\\/(?<options>[^:]+))?:(?<text>.+?)(?<!\\\\)\\]", RegexOptions.Compiled);
			MatchCollection matches = regex.Matches(s);

			foreach (Match m in matches)
			{
				Tag tag = new Tag(m);
				s = s.Replace(m.Value, tag.Parse(quoteResult));
			}

			return s;
		}
	}
}
