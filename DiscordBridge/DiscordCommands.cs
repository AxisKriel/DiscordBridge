using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;
using DiscordBridge.Framework;
using TShockAPI;
using TSCommand = TShockAPI.Command;

namespace DiscordBridge
{
	public partial class DiscordBridge
	{
		private void initDiscordCommands()
		{
			// Note: ParameterType.Unparsed catches all remaining text as a single optional parameter
			Client.GetService<CommandService>().CreateCommand("do")
				.Alias("execute", "run")
				.Description("Executes a TShock command.")
				.Parameter("command", ParameterType.Required)
				.Parameter("parameters", ParameterType.Unparsed)
				.Do(async e =>
				{
					BridgeUser player = Client[e.User];
					if (player == null)
					{
						await e.User.SendMessage("You must be logged in to use TShock commands.");
						await e.User.SendMessage($"Message me with `{Config.BotPrefix}login <username> <password>` using your TShock credentials to begin.");
						return;
					}

					// Blacklist commands which must be run through their discord command counterparts
					var blacklist = new List<string>
					{
						"login",
						"logout"
					};

					if (blacklist.Contains(e.GetArg("command")))
					{
						await e.Channel.SendMessage($"Use `{Config.BotPrefix}{e.GetArg("command")}` without the `{Config.BotPrefix}do` prefix instead.");
						return;
					}

					TSCommand command = Commands.ChatCommands.Find(c => !c.Names.Contains("login")
						&& !c.Names.Contains("logout")
						&& c.Names.Contains(e.GetArg("command")));

					if (command == null)
					{
						await e.Channel.SendMessage($"`{e.GetArg("command")}` is not a TShock command.");
						return;
					}

					var sb = new StringBuilder();

					if (!e.GetArg("command").StartsWith(Commands.Specifier) && !e.GetArg("command").StartsWith(Commands.Specifier))
						sb.Append(Commands.Specifier);

					// Temporarily set their command channel so that messages end in the right place
					player.CommandChannel = e.Channel;
					Commands.HandleCommand(player, sb.Append(e.GetArg("command")).Append(' ').Append(e.GetArg("parameters")).ToString());
					player.CommandChannel = null;
				});

			#region Account Commands

			Client.GetService<CommandService>().CreateCommand("login")
				.Description("Log in to a TShock user account to use its permissions when using the `do` command.")
				.Parameter("username", ParameterType.Required)
				.Parameter("password", ParameterType.Required)
				.Do(async e =>
				{
					if (Client[e.User] != null)
					{
						await e.User.SendMessage($"You are already logged in. Use `{Config.BotPrefix}logout` first if you wish to log in to a different account.");
						return;
					}

					string username = e.GetArg("username");
					string password = e.GetArg("password");

					TShockAPI.DB.User user = TShock.Users.GetUserByName(username);
					if (user == null)
						await e.User.SendMessage("A user by that name does not exist.");

					else if (!user.VerifyPassword(password))
						await e.User.SendMessage("Invalid password!");

					else
					{
						BridgeUser bridgeUser = new BridgeUser(user, e.User);

						// Do whatever needs to be done when a user initialized here
						bridgeUser.IsLoggedIn = true;

						// Future implementation: store this information (discordUserId: tshockUserId) somewhere for auto-login
						Client[e.User] = bridgeUser;
						await e.User.SendMessage($"Authenticated as {bridgeUser.Name} successfully.");
					}
				});

			Client.GetService<CommandService>().CreateCommand("logout")
				.Description("Log out of your current TShock user account.")
				.Do(async e =>
				{
					if (Client[e.User] == null)
					{
						await e.User.SendMessage("You are not logged in.");
						return;
					}

					// Future implementation: remove stored information
					Client[e.User] = null;
					await e.User.SendMessage("You have been successfully logged out of your account.");
				});

			#endregion

			#region Administrative Commands

			Client.GetService<CommandService>().CreateCommand("echo")
				.Alias("bc", "say")
				.Description("Make this bot say something.")
				.Parameter("text", ParameterType.Unparsed)
				.AddCheck((cmd, user, channel) => user.ServerPermissions.Administrator)
				.Do(async e => await e.Channel.SendMessage(e.GetArg("text") ?? "Hi!"));

			Client.GetService<CommandService>().CreateCommand("addbot")
				.Description("Add another connected Discord Bridge bot to the list of ServerBots for multi-server broadcasting.")
				.Parameter("name", ParameterType.Required)
				.AddCheck((cmd, user, channel) => user.ServerPermissions.Administrator)
				.Do(async e =>
				{
					User botUser = Client.CurrentServer.FindUsers(e.GetArg("name"), true).FirstOrDefault();

					if (botUser == null
					&& (botUser = Client.CurrentServer.Users.FirstOrDefault(u => u.Nickname.Equals(e.GetArg("name"), StringComparison.OrdinalIgnoreCase))) == null)
					{
						await e.Channel.SendMessage($"User `{e.GetArg("name")}` is not on this server.");
						return;
					}

					if (!botUser.IsBot)
					{
						await e.Channel.SendMessage($"`{e.GetArg("name")}` is not a bot.");
						return;
					}

					if (Config.OtherServerBots.Exists(b => b.Id == botUser.Id))
					{
						await e.Channel.SendMessage($"`{e.GetArg("name")}` is already on the broadcast is.");
						return;
					}

					// Because ConfigFile only saves when directly setting a member, we need to re-set the entire list here
					var bots = new List<ConfigFile.ServerBot>(Config.OtherServerBots);
					bots.Add(new ConfigFile.ServerBot { Id = botUser.Id });
					Config.OtherServerBots = bots;

					string mention = String.IsNullOrWhiteSpace(botUser.Nickname) ? botUser.Mention : botUser.NicknameMention;
					await e.Channel.SendMessage($"Added {mention} to the broadcasting list.");
				});

			#endregion
		}
	}
}
