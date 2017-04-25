using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using TShockAPI;
using Color = Microsoft.Xna.Framework.Color;

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
					try
					{
						Message m = await CommandChannel.SendMessage(String.Join("\n", _messages));
						if (m?.State == MessageState.Failed)
							TShock.Log.Error($"discord-bridge: Message broadcasting to channel '{CommandChannel.Name}' failed!");
				}
					catch (Exception ex)
					{
						TShock.Log.Error(ex.ToString());
					}
				
				_messages.Clear();
			}
		}

		public override void SendErrorMessage(string msg)
		{
			SendMessage(msg);
		}

		public override void SendInfoMessage(string msg)
		{
			SendMessage(msg);
		}

		public override void SendSuccessMessage(string msg)
		{
			SendMessage(msg);
		}

		public override void SendWarningMessage(string msg)
		{
			SendMessage(msg);
		}

		public override void SendMessage(string msg, Color color)
		{
			SendMessage(msg);
		}

		public override void SendMessage(string msg, byte red, byte green, byte blue)
		{
			// Maybe one day Discord will support custom message colors
			SendMessage(msg);
		}

		/// <summary>
		/// Queues a message to be sent to this discord client.
		/// </summary>
		/// <param name="msg">Text to send.</param>
		public void SendMessage(string msg)
		{
			if (AutoFlush)
				Task.Run(async () =>
				{
					try
					{
						Message m = await CommandChannel.SendMessage(msg);
						if (m?.State == MessageState.Failed)
							TShock.Log.Error($"discord-bridge: Message broadcasting to channel '{CommandChannel.Name}' failed!");
					}
					catch (Exception ex)
					{
						TShock.Log.Error(ex.ToString());
					}
				});
			else
				_messages.Add(msg);
		}
	}
}
