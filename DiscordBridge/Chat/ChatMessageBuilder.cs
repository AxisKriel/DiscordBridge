using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DiscordBridge.Chat
{
	public class ChatMessageBuilder
	{
		private ChatMessage _message;

		/// <summary>
		/// The message base color.
		/// </summary>
		public Color? Color => _message.Color;

		/// <summary>
		/// Name of the sender.
		/// </summary>
		public ChatMessage.Section Name => _message.Name;

		/// <summary>
		/// Message header, replaces TShock's Group.Name parameter.
		/// </summary>
		public ChatMessage.Section Header => _message.Header;

		/// <summary>
		/// A list of prefixes for use in formatting.
		/// </summary>
		public List<ChatMessage.Section> Prefixes => _message.Prefixes;

		/// <summary>
		/// A list of suffixes for use in formatting.
		/// </summary>
		public List<ChatMessage.Section> Suffixes => _message.Suffixes;

		/// <summary>
		/// The message text.
		/// </summary>
		public string Text => _message.Text;

		/// <summary>
		/// The default chat format used by TShock.
		/// </summary>
		public static string DefaultChatFormat => "{1}{2}{3}: {4}";

		/// <summary>
		/// The chat format used to format the message.
		/// </summary>
		public string Format { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMessageBuilder"/> class.
		/// </summary>
		internal ChatMessageBuilder()
		{
			_message = new ChatMessage();
			Format = DefaultChatFormat;
		}

		/// <summary>
		/// Appends some text to the message's body.
		/// </summary>
		/// <param name="textToAppend">The text to append.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Append(string textToAppend)
		{
			_message.Text += textToAppend;
			return this;
		}

		/// <summary>
		/// Sets the base message color.
		/// </summary>
		/// <param name="color">The color to render this message with. Color tags will override specific parts of it.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Colorize(Color? color)
		{
			_message.Color = color;
			return this;
		}

		/// <summary>
		/// Sets the string used to format the message:
		/// {0} = Header, {1} = Prefix(es), {2} = Name, {3} = Suffix(es), {4} = Message Body.
		/// </summary>
		/// <param name="format">The format string.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder SetFormat(string format)
		{
			Format = format;
			return this;
		}

		/// <summary>
		/// Sets the message header.
		/// </summary>
		/// <param name="name">Message header (or group name).</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder SetHeader(string header)
		{
			_message.Header = new ChatMessage.Section { Text = header };
			return this;
		}

		/// <summary>
		/// Sets the message header.
		/// </summary>
		/// <param name="name">Message header (or group name).</param>
		/// <param name="color">The color for this message section.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder SetHeader(string header, Color color)
		{
			_message.Header = new ChatMessage.Section { Text = header, Color = color };
			return this;
		}

		/// <summary>
		/// Sets the name of the sender.
		/// </summary>
		/// <param name="name">Name of the sender, usually a player.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder SetName(string name)
		{
			_message.Name = new ChatMessage.Section { Text = name };
			return this;
		}

		/// <summary>
		/// Sets the name of the sender.
		/// </summary>
		/// <param name="name">Name of the sender, usually a player.</param>
		/// <param name="color">The color for this message section.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder SetName(string name, Color color)
		{
			_message.Name = new ChatMessage.Section { Text = name, Color = color };
			return this;
		}

		/// <summary>
		/// Sets the message body.
		/// </summary>
		/// <param name="text">The message text.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder SetText(string text)
		{
			_message.Text = text;
			return this;
		}

		/// <summary>
		/// Adds a prefix to the list of prefixes to render.
		/// </summary>
		/// <param name="prefix">The prefix to add.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Prefix(string prefix)
		{
			// Don't add empty values
			if (!String.IsNullOrWhiteSpace(prefix))
				_message.Prefixes.Add(new ChatMessage.Section { Text = prefix });
			return this;
		}

		/// <summary>
		/// Adds a prefix to the list of prefixes to render.
		/// </summary>
		/// <param name="prefix">The prefix to add.</param>
		/// <param name="color">The color for this prefix.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Prefix(string prefix, Color color)
		{
			// Don't add empty values
			if (!String.IsNullOrWhiteSpace(prefix))
				_message.Prefixes.Add(new ChatMessage.Section { Text = prefix, Color = color });
			return this;
		}

		/// <summary>
		/// Adds multiple prefixes to the list of prefixes to render.
		/// </summary>
		/// <param name="prefixes">The prefixes to add.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Prefix(IEnumerable<ChatMessage.Section> prefixes)
		{
			foreach (var section in prefixes)
				_message.Prefixes.Add(section);
			return this;
		}

		/// <summary>
		/// Sets the string used to separate prefixes.
		/// Default: Whitespace
		/// </summary>
		/// <param name="separator"></param>
		/// <returns></returns>
		public ChatMessageBuilder PrefixSeparator(string separator)
		{
			_message.PrefixSeparator = separator;
			return this;
		}

		/// <summary>
		/// Adds a suffix to the list of suffixes to render.
		/// </summary>
		/// <param name="suffix">The suffix to add.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Suffix(string suffix)
		{
			// Don't add empty values
			if (!String.IsNullOrWhiteSpace(suffix))
				_message.Suffixes.Add(new ChatMessage.Section { Text = suffix });
			return this;
		}

		/// <summary>
		/// Adds a suffix to the list of suffixes to render.
		/// </summary>
		/// <param name="suffix">The suffix to add.</param>
		/// <param name="color">The color for this suffix.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Suffix(string suffix, Color color)
		{
			// Don't add empty values
			if (!String.IsNullOrWhiteSpace(suffix))
				_message.Suffixes.Add(new ChatMessage.Section { Text = suffix, Color = color });
			return this;
		}

		/// <summary>
		/// Adds multiple suffixes to the list of suffixes to render.
		/// </summary>
		/// <param name="suffixes">The suffixes to add.</param>
		/// <returns>The builder instance.</returns>
		public ChatMessageBuilder Suffix(IEnumerable<ChatMessage.Section> suffixes)
		{
			foreach (var section in suffixes)
				_message.Suffixes.Add(section);
			return this;
		}

		/// <summary>
		/// Sets the string used to separate suffixes.
		/// Default: Whitespace
		/// </summary>
		/// <param name="separator"></param>
		/// <returns></returns>
		public ChatMessageBuilder SuffixSeparator(string separator)
		{
			_message.SuffixSeparator = separator;
			return this;
		}

		public ChatMessage ToMessage() => _message;

		public override string ToString() => String.Format(Format,
			Header,
			String.Join(" ", Prefixes),
			Name,
			String.Join(" ", Suffixes),
			Text).Trim();
	}
}
