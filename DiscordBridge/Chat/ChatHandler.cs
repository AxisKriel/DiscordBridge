using System;
using DiscordBridge.Extensions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace DiscordBridge.Chat
{
	public class ChatHandler
	{
		/// <summary>
		/// Occurs just before chat is broadcasted to players.
		/// </summary>
		public static event EventHandler<PlayerChattingEventArgs> PlayerChatting;

		/// <summary>
		/// Occurs after chat has been broadcasted.
		/// </summary>
		public static event EventHandler<PlayerChattedEventArgs> PlayerChatted;

		internal Color? ChatColorOverride => Config?.ChatColorOverride.FromRGB();

		internal ConfigFile Config { get; set; }

		internal bool StripTagsFromConsole => Config.StripTagsFromConsole;

		public static ChatMessageBuilder CreateMessage(string format)
		{
			return new ChatMessageBuilder().SetFormat(format);
		}

		internal void Handle(ServerChatEventArgs e)
		{
			if (e.Handled)
				return;

			// Most of the following checks have been taken from TShock to prevent duplicate work
			var tsplr = TShock.Players[e.Who];
			if (tsplr == null)
			{
				e.Handled = true;
				return;
			}

			if (e.Text.Length > 500)
			{
				TShock.Utils.Kick(tsplr, "Crash attempt via long chat packet.", true);
				e.Handled = true;
				return;
			}

			if ((e.Text.StartsWith(Commands.Specifier) || e.Text.StartsWith(Commands.SilentSpecifier))
				&& !String.IsNullOrWhiteSpace(e.Text.Substring(1)))
			{
				try
				{
					e.Handled = Commands.HandleCommand(tsplr, e.Text);
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("An exception occurred executing a command.");
					TShock.Log.Error(ex.ToString());
				}
			}
			else
			{
				if (!tsplr.HasPermission(TShockAPI.Permissions.canchat))
				{
					e.Handled = true;
				}
				else if (tsplr.mute)
				{
					tsplr.SendErrorMessage("You are muted!");
					e.Handled = true;
				}
				else if (!TShock.Config.EnableChatAboveHeads)
				{
					var args = new PlayerChattingEventArgs(
						CreateMessage(TShock.Config.ChatFormat)
							.SetHeader(tsplr.Group.Name)
							.SetName(tsplr.Name)
							.Prefix(tsplr.Group.Prefix)
							.Suffix(tsplr.Group.Suffix)
							.SetText(e.Text)
							.Colorize(new Color(tsplr.Group.R, tsplr.Group.G, tsplr.Group.B)),
						tsplr, e.Text);

					/* Setting up the color dictionary - if there is need for more colors, they may be added later
					 * Plugins may add more formatters by adding elements to the ColorFormatters property of event args */
					args.ColorFormatters.Add("Group", new Color(tsplr.Group.R, tsplr.Group.G, tsplr.Group.B));
					args.ColorFormatters.Add("Message", args.Message.Color);
					args.ColorFormatters.Add("Name", args.Message.Name.Color);

					PlayerChatting?.Invoke(this, args);

					// Manually replicate Broadcast so that the message sent to the console doesn't include tags
					TSPlayer.All.SendMessage(args.Message.ToString().FormatChat(args.ChatFormatters).ParseColors(args.ColorFormatters),
						ChatColorOverride ?? args.Message.Color ?? Color.White);

					if (StripTagsFromConsole)
						TSPlayer.Server.SendMessage(args.Message.ToString().FormatChat(args.ChatFormatters).StripTags(), ChatColorOverride ?? args.Message.Color ?? Color.White);
					else
						TSPlayer.Server.SendMessage(args.Message.ToString().FormatChat(args.ChatFormatters), ChatColorOverride ?? args.Message.Color ?? Color.White);

					TShock.Log.Info($"Broadcast: {args.Message.ToString()}");

					PlayerChatted?.Invoke(this, new PlayerChattedEventArgs(args));
					e.Handled = true;
				}
				else
				{
					// I can't help but think ChatAboveHeads is messed up
					Player ply = Main.player[e.Who];
					string name = ply.name;
					ply.name = CreateMessage(TShock.Config.ChatAboveHeadsFormat)
						.SetHeader(tsplr.Group.Name)
						.SetName(tsplr.Name)
						.Prefix(tsplr.Group.Prefix)
						.Suffix(tsplr.Group.Suffix)
						.ToString();

					NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, new NetworkText(ply.name, NetworkText.Mode.Literal), e.Who, 0, 0, 0, 0);
					ply.name = name;
					var args = new PlayerChattingEventArgs(e.Text);
					PlayerChatting?.Invoke(this, args);

					Color color = ChatColorOverride ?? args.Message.Color ?? new Color(tsplr.Group.R, tsplr.Group.G, tsplr.Group.B);
					NetMessage.SendData((int)PacketTypes.ChatText, -1, e.Who,
						new NetworkText(args.Message.ToString().FormatChat(args.ChatFormatters).ParseColors(args.ColorFormatters), NetworkText.Mode.Literal), e.Who, color.R, color.G, color.B);
					NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, new NetworkText(name, NetworkText.Mode.Literal), e.Who, 0, 0, 0, 0);

					var msg = CreateMessage("<{2}> {4}").SetName(CreateMessage(TShock.Config.ChatAboveHeadsFormat)
						.SetHeader(tsplr.Group.Name)
						.Prefix(tsplr.Group.Prefix)
						.SetName(tsplr.Name)
						.Suffix(tsplr.Group.Suffix).ToString()).SetText(args.Message.ToString());

					tsplr.SendMessage(msg.ToString(), color.R, color.G, color.B);
					PlayerChatted?.Invoke(this, new PlayerChattedEventArgs(args));

					TSPlayer.Server.SendMessage(msg.ToString(), color.R, color.G, color.B);
					TShock.Log.Info("Broadcast: {0}", msg);
					e.Handled = true;
				}
			}
		}
	}
}
