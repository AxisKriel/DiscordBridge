using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using TShockAPI;
using TShockAPI.DB;

namespace DiscordBridge.Framework
{
	public class BridgeUser : TSPlayer
	{
		private List<string> _messages = new List<string>();
		private Channel _channel;

		public bool AutoFlush { get; set; } = true;

		public Channel CommandChannel
		{
			get { return _channel ?? DiscordUser.PrivateChannel; }
			set { _channel = value; }
		}

		public Discord.User DiscordUser { get; }

		private BridgeUser(TShockAPI.DB.User user) : base(user.Name)
		{
			Group = TShock.Utils.GetGroup(user.Group);
			User = user;
		}

		public BridgeUser(Discord.User discordUser) : base(discordUser.Name)
		{
			DiscordUser = discordUser;
		}

		public BridgeUser(TShockAPI.DB.User user, Discord.User discordUser) : this(user)
		{
			DiscordUser = discordUser;
			IsLoggedIn = true;
		}

		public async Task FlushMessages()
		{
			if (_messages.Count > 0)
			{
				await CommandChannel.SendMessage(String.Join("\n", _messages));
				_messages.Clear();
			}
		}

		public override async void SendErrorMessage(string msg)
		{
			if (AutoFlush)
				await CommandChannel.SendMessage(msg);
			else
				_messages.Add(msg);
		}

		public override async void SendInfoMessage(string msg)
		{
			if (AutoFlush)
				await CommandChannel.SendMessage(msg);
			else
				_messages.Add(msg);
		}

		public override async void SendSuccessMessage(string msg)
		{
			if (AutoFlush)
				await CommandChannel.SendMessage(msg);
			else
				_messages.Add(msg);
		}

		public override async void SendWarningMessage(string msg)
		{
			if (AutoFlush)
				await CommandChannel.SendMessage(msg);
			else
				_messages.Add(msg);
		}

		public override async void SendMessage(string msg, Color color)
		{
			if (AutoFlush)
				await CommandChannel.SendMessage(msg);
			else
				_messages.Add(msg);
		}

		public override async void SendMessage(string msg, byte red, byte green, byte blue)
		{
			// Maybe one day Discord will support custom message colors
			if (AutoFlush)
				await CommandChannel.SendMessage(msg);
			else
				_messages.Add(msg);
		}
	}
}
