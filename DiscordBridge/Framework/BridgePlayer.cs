using TShockAPI;
using TShockAPI.DB;
using Discord;

namespace DiscordBridge.Framework
{
	public class BridgeUser : TSPlayer
	{
		public Channel CommandChannel { get; set; }

		public Discord.User DiscordUser { get; }

		private BridgeUser(TShockAPI.DB.User user) : base(user.Name)
		{
			Group = TShock.Utils.GetGroup(user.Group);
			User = user;
		}

		public BridgeUser(TShockAPI.DB.User user, Discord.User discordUser) : this(user)
		{
			DiscordUser = discordUser;
		}

		public override async void SendErrorMessage(string msg)
		{
			await (CommandChannel?.SendMessage(msg) ?? DiscordUser.SendMessage(msg));
		}

		public override async void SendInfoMessage(string msg)
		{
			await (CommandChannel?.SendMessage(msg) ?? DiscordUser.SendMessage(msg));
		}

		public override async void SendSuccessMessage(string msg)
		{
			await (CommandChannel?.SendMessage(msg) ?? DiscordUser.SendMessage(msg));
		}

		public override async void SendWarningMessage(string msg)
		{
			await (CommandChannel?.SendMessage(msg) ?? DiscordUser.SendMessage(msg));
		}

		public override async void SendMessage(string msg, Color color)
		{
			await (CommandChannel?.SendMessage(msg) ?? DiscordUser.SendMessage(msg));
		}

		public override async void SendMessage(string msg, byte red, byte green, byte blue)
		{
			// Maybe one day Discord will support custom message colors
			await (CommandChannel?.SendMessage(msg) ?? DiscordUser.SendMessage(msg));
		}
	}
}
