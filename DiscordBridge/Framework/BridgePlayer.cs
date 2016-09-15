using TShockAPI;
using TShockAPI.DB;

namespace DiscordBridge.Framework
{
	public class BridgeUser : TSPlayer
	{
		public Discord.User DiscordUser { get; }

		private BridgeUser(User user) : base(user.Name)
		{
			Group = TShock.Utils.GetGroup(user.Group);
			User = user;
		}

		public BridgeUser(User user, Discord.User discordUser) : this(user)
		{
			DiscordUser = discordUser;
		}

		public override async void SendErrorMessage(string msg)
		{
			await DiscordUser.SendMessage(msg);
		}

		public override async void SendInfoMessage(string msg)
		{
			await DiscordUser.SendMessage(msg);
		}

		public override async void SendSuccessMessage(string msg)
		{
			await DiscordUser.SendMessage(msg);
		}

		public override async void SendWarningMessage(string msg)
		{
			await DiscordUser.SendMessage(msg);
		}

		public override async void SendMessage(string msg, Color color)
		{
			await DiscordUser.SendMessage(msg);
		}

		public override async void SendMessage(string msg, byte red, byte green, byte blue)
		{
			// Maybe one day Discord will support custom message colors
			await DiscordUser.SendMessage(msg);
		}
	}
}
