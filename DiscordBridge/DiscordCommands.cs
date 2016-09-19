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
					BridgeUser player = await Client.LoadUser(e.User);

					if (!player.IsLoggedIn)
					{
						await e.User.SendMessage("You must be logged in to use TShock commands.\n"
							+ $"Message me with `{Config.BotPrefix}login <username> <password>` using your TShock credentials to begin.");
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
						await e.Channel.SendMessage($"This is a discord command, so use `{Config.BotPrefix}{e.GetArg("command")}` (without the `{Config.BotPrefix}do` prefix) instead.");
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

					// Disable auto flush to reduce consumption of the discord API for multiple messages
					player.AutoFlush = false;

					if (Commands.HandleCommand(player, sb.Append(e.GetArg("command")).Append(' ').Append(e.GetArg("parameters")).ToString()))
						await player.FlushMessages();
					else
						await e.Channel.SendMessage("Command failed, check logs for details.");

					player.AutoFlush = true;
					player.CommandChannel = null;
				});

			#region Account Commands

			Client.GetService<CommandService>().CreateCommand("login")
				.Description("Log in to a TShock user account to use its permissions when using the `do` command.")
				.Parameter("username", ParameterType.Required)
				.Parameter("password", ParameterType.Required)
				.Do(async e =>
				{
					BridgeUser player = await Client.LoadUser(e.User);

					if (e.Channel != e.User.PrivateChannel)
					{
						// Delete the message
						await e.Message.Delete();
					}

					if (player.IsLoggedIn)
					{
						await e.Channel.SendMessage($"You are already logged in. Use `{Config.BotPrefix}logout` first if you wish to log in to a different account.");
						return;
					}

					string username = e.GetArg("username");
					string password = e.GetArg("password");

					TShockAPI.DB.User user = TShock.Users.GetUserByName(username);
					if (user == null)
						await e.Channel.SendMessage("A user by that name does not exist.");

					else if (!user.VerifyPassword(password))
						await e.Channel.SendMessage("Invalid password!");

					else
					{
						await Logins.SetData(e.User, user);
						player = await Logins.Authenticate(e.User.Id);
						await e.Channel.SendMessage($"Authenticated as {player.Name} successfully.");
					}
				});

			Client.GetService<CommandService>().CreateCommand("logout")
				.Description("Log out of your current TShock user account.")
				.Do(async e =>
				{
					BridgeUser player = await Client.LoadUser(e.User);

					if (!player.IsLoggedIn)
					{
						await e.Channel.SendMessage("You are not logged in.");
						return;
					}

					await Logins.RemoveData(e.User.Id);
					await Logins.Authenticate(e.User.Id);
					await e.Channel.SendMessage("You have been successfully logged out of your account.");
				});

			#endregion

			#region Administrative Commands

			Client.GetService<CommandService>().CreateCommand("echo")
				.Alias("bc", "say")
				.Description("Make this bot say something.")
				.Parameter("text", ParameterType.Unparsed)
				.AddCheck((c, u, ch) => !ch.IsPrivate)
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

					string mention = String.IsNullOrWhiteSpace(botUser.Nickname) ? botUser.Mention : botUser.NicknameMention;
					if (Config.AddBot(botUser.Id))
						await e.Channel.SendMessage($"Added {mention} to the broadcasting list.");
					else
						await e.Channel.SendMessage($"{mention} is already on the broadcast list.");
				});

			#endregion
		}
	}
}
