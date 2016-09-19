using System;
using System.Collections.Generic;
using System.Timers;
using Discord;
using TShockAPI;
using TShockAPI.DB;

namespace DiscordBridge.Framework
{
	public class BridgeUser : TSPlayer
	{
		private object _sending = new object();
		private List<string> _messages = new List<string>();
		private Timer _updateTimer = new Timer(500);

		public Channel CommandChannel { get; set; }

		public Discord.User DiscordUser { get; }

		private BridgeUser(TShockAPI.DB.User user) : base(user.Name)
		{
			Group = TShock.Utils.GetGroup(user.Group);
			User = user;

			_updateTimer.Elapsed += flushMessages;
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

		~BridgeUser()
		{
			_updateTimer.Elapsed -= flushMessages;
			_updateTimer.Stop();
		}

		private async void flushMessages(object sender, ElapsedEventArgs e)
		{
			string fullMsg;
			lock (_sending)
			{
				fullMsg = String.Join("\n", _messages);
				_messages.Clear();
				_updateTimer.Stop();
			}
			await (CommandChannel?.SendMessage(fullMsg) ?? DiscordUser.SendMessage(fullMsg));
			CommandChannel = null;
		}

		public override void SendErrorMessage(string msg)
		{
			lock (_sending)
			{
				_messages.Add(msg);
				if (!_updateTimer.Enabled)
					_updateTimer.Start();
			}
		}

		public override void SendInfoMessage(string msg)
		{
			lock (_sending)
			{
				_messages.Add(msg);
				if (!_updateTimer.Enabled)
					_updateTimer.Start();
			}
		}

		public override void SendSuccessMessage(string msg)
		{
			lock (_sending)
			{
				_messages.Add(msg);
				if (!_updateTimer.Enabled)
					_updateTimer.Start();
			}
		}

		public override void SendWarningMessage(string msg)
		{
			lock (_sending)
			{
				_messages.Add(msg);
				if (!_updateTimer.Enabled)
					_updateTimer.Start();
			}
		}

		public override void SendMessage(string msg, Color color)
		{
			lock (_sending)
			{
				_messages.Add(msg);
				if (!_updateTimer.Enabled)
					_updateTimer.Start();
			}
		}

		public override void SendMessage(string msg, byte red, byte green, byte blue)
		{
			// Maybe one day Discord will support custom message colors
			lock (_sending)
			{
				_messages.Add(msg);
				if (!_updateTimer.Enabled)
					_updateTimer.Start();
			}
		}
	}
}
