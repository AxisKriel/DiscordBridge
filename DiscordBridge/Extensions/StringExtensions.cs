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
			// Source: Terraria
			public static Regex Regex = new Regex("(?<!\\\\)\\[(?<tag>[a-zA-Z]{1,10})(\\/(?<options>[^:]+))?:(?<text>.+?)(?<!\\\\)\\]", RegexOptions.Compiled);

			public enum TagType
			{
				None,
				Color,
				Item,
				Name,
				Achievement,
				Glyph
			}

			private string _raw { get; }

			public TagType Type { get; }

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
						Type = TagType.Color;
						break;

					case "i":
					case "item":
						Type = TagType.Item;
						break;

					case "n":
					case "name":
						Type = TagType.Name;
						break;

					case "a":
					case "achievement":
						Type = TagType.Achievement;
						break;

					case "g":
					case "glyph":
						Type = TagType.Glyph;
						break;

					default:
						Type = TagType.None;
						break;
				}

				Options = options;
				Text = text;
			}

			public string Parse(bool quotes = false)
			{
				switch (Type)
				{
					case TagType.None:
					case TagType.Color:
					case TagType.Name:
					// Sadly there is no way left in TSAPI for getting an achievement name, so we have to keep it as the tag text
					case TagType.Achievement:
					case TagType.Glyph:
					default:
						return Text;

					case TagType.Item:
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

		public static string FormatChat(this string s, Dictionary<string, string> chatDictionary)
		{
			MatchCollection matches = Regex.Matches(s, @"{([a-zA-Z]+)}");

			foreach (Match m in matches)
			{
				if (chatDictionary.ContainsKey(m.Groups[1].Value))
					s = s.Replace(m.Value, chatDictionary[m.Groups[1].Value]);
				else
					s = s.Replace(m.Value, "");
			}

			return s;
		}

		/// <summary>
		/// Converts all color tags in a string that use a variable name as the option into regular color tags.
		/// </summary>
		/// <param name="s">The string to parse.</param>
		/// <param name="colorDictionary">
		/// A dictionary where the keys are variable names and the values are the corresponding colors.
		/// </param>
		/// <returns>The input string after being parsed.</returns>
		public static string ParseColors(this string s, Dictionary<string, Color?> colorDictionary)
		{
			MatchCollection matches = Tag.Regex.Matches(s);

			foreach (Match m in matches)
			{
				Tag tag = new Tag(m);
				if (tag.Type == Tag.TagType.Color && colorDictionary.ContainsKey(tag.Options))
				{
					if (colorDictionary[tag.Options].HasValue)
						s = s.Replace(m.Value, $"[c/{colorDictionary[tag.Options].Value.Hex3()}:{tag.Text}]");
					else
						s = s.Replace(m.Value, tag.Text);
				}
			}

			return s;
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
			MatchCollection matches = Tag.Regex.Matches(s);

			foreach (Match m in matches)
			{
				Tag tag = new Tag(m);
				s = s.Replace(m.Value, tag.Parse(quoteResult));
			}

			return s;
		}
	}
}
