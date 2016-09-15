using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBridge.Extensions;
using DiscordBridge.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using TSCommand = TShockAPI.Command;

namespace DiscordBridge
{
	[ApiVersion(1, 23)]
	public partial class DiscordBridge : TerrariaPlugin
	{
		public override string Author => "Enerdy";

		public BridgeClient Client { get; private set; }

		public ConfigFile Config { get; private set; }

		public override string Description => "Connects Terraria to a Discord server.";

		public override string Name => "Discord Bridge";

		public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

		public DiscordBridge(Main game) : base(game)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, onPostInitialize);

				PlayerHooks.PlayerChat -= onChat;

				Client.Dispose();
			}
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
			ServerApi.Hooks.GamePostInitialize.Register(this, onPostInitialize);

			PlayerHooks.PlayerChat += onChat;
		}

		private async void onChat(PlayerChatEventArgs e)
		{
			if (e.Handled)
				return;

			if (Client.State == ConnectionState.Connected)
			{
				// Todo: figure out multi server chat here
				foreach (string s in Config.TerrariaChannels)
				{
					Channel c = Client.CurrentServer.FindChannels(s, exactMatch: true).FirstOrDefault();
					if (c != null)
					{
						Message m = await c.SendMessage(e.TShockFormattedText.StripTags());
						if (m.State == MessageState.Failed)
						{
							TShock.Log.ConsoleError($"discord-bridge: Message broadcasting to channel '{c.Name}' failed!");
						}
					}
				}
			}
		}

		private void onInitialize(EventArgs args)
		{
			Config = ConfigFile.Read();

			Commands.ChatCommands.Add(new TSCommand(Permissions.Use, doDiscord, "bridge", "discord"));

			Client = new BridgeClient(this);

			// Install the command service
			Client.UsingCommands(x =>
			{
				x.PrefixChar = Config.BotPrefix;
				x.HelpMode = HelpMode.Private;
			});

			#region Discord Commands

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
						await e.User.SendMessage("Message me with `login <username> <password>` using your TShock credentials to begin.");
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

					Commands.HandleCommand(player, sb.Append(e.GetArg("command")).Append(' ').Append(e.GetArg("parameters")).ToString());
				});

			Client.GetService<CommandService>().CreateCommand("login")
				.Description("Log in to a TShock user account to use its permissions when using the `do` command.")
				.Parameter("username", ParameterType.Required)
				.Parameter("password", ParameterType.Required)
				.Do(async e =>
				{
					if (Client[e.User] != null)
					{
						await e.User.SendMessage("You are already logged in. Use `logout` first if you wish to log in to a different account.");
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
		}

		private async void onPostInitialize(EventArgs e)
		{
			await Client.StartUp();

			if (Client.State == ConnectionState.Connected)
				TShock.Log.ConsoleInfo(" * Discord Bridge bot connected.");
			else if (String.IsNullOrWhiteSpace(Config.BotToken))
			{
				TShock.Log.ConsoleInfo(" * Discord bot token was not found in the config.");
				TShock.Log.ConsoleInfo(" * To enable the bot, run 'discord set-token <bot token>'.");
			}
			else
			{
				TShock.Log.ConsoleError(" * Discord bot is NOT connected. Check your internet connection and try again.");
			}
		}
	}
}
