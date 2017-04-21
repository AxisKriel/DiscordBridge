using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using DiscordBridge.Chat;
using DiscordBridge.Extensions;
using DiscordBridge.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Color = Microsoft.Xna.Framework.Color;

namespace DiscordBridge
{
	[ApiVersion(2, 1)]
	public partial class DiscordBridge : TerrariaPlugin
	{
		public override string Author => "Enerdy";

		/// <summary>
		/// The purpose of ChatHandler is to take away TShock's control over chat so that it may be formatted properly.
		/// Any plugin that wishes to modify chat in any way must hook to <see cref="ChatHandler.PlayerChatting"/> and use
		/// the <see cref="ChatMessageBuilder"/> methods to modify its Message property.
		/// </summary>
		public ChatHandler ChatHandler { get; }

		public BridgeClient Client { get; private set; }

		public ConfigFile Config { get; private set; }

		public override string Description => "Connects Terraria to a Discord server.";

		public LoginManager Logins { get; private set; }

		public override string Name => "Discord Bridge";

		public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

		public DiscordBridge(Main game) : base(game)
		{
			ChatHandler = new ChatHandler();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, onPostInitialize);

				ServerApi.Hooks.NetGreetPlayer.Deregister(this, onGreet);
				ServerApi.Hooks.ServerChat.Deregister(this, ChatHandler.Handle);
				ServerApi.Hooks.ServerLeave.Deregister(this, onLeave);

				ChatHandler.PlayerChatted -= onChat;

				Client.Dispose();
			}
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
			ServerApi.Hooks.GamePostInitialize.Register(this, onPostInitialize);

			ServerApi.Hooks.NetGreetPlayer.Register(this, onGreet);
			ServerApi.Hooks.ServerChat.Register(this, ChatHandler.Handle, 1);
			ServerApi.Hooks.ServerLeave.Register(this, onLeave);

			ChatHandler.PlayerChatted += onChat;;
		}

		private async void onChat(object sender, PlayerChattedEventArgs e)
		{
			if (Client.State == ConnectionState.Connected)
			{
				foreach (string s in Config.TerrariaChannels)
				{
					Channel c = Client.CurrentServer.FindChannels(s, exactMatch: true).FirstOrDefault();
					if (c != null)
					{
						try
						{
							Message m = await c.SendMessage(e.Message.SetFormat(Config.DiscordChatFormat).ToString().FormatChat(e.ChatFormatters).StripTags(true));
							if (m.State == MessageState.Failed)
								throw new Exception();
						}
						catch
						{
							TShock.Log.ConsoleError($"discord-bridge: Message broadcasting to channel '{c.Name}' failed!");
						}
					}
				}

				// Multi-Server Broadcast
				foreach (ConfigFile.ServerBot bot in Config.ServerBots.FindAll(b => b.Id > 0))
				{
					User botUser = Client.CurrentServer.GetUser(bot.Id);

					if (botUser == null || !botUser.IsBot /* Apparently bots can be "Offline" while being connected?? || botUser.Status == UserStatus.Offline*/)
					{
						// We only support active bots, mang
						Client.Log.Warning("OnChat", $"Broadcasting to bot {bot.Id} failed (null: {botUser == null} | IsBot: {botUser?.IsBot} | Status: {botUser?.Status?.Value})");
						return;
					}

					var colorDictionary = e.ColorFormatters;

					#region Format Colors

					var botNick = new ChatMessage.Section(String.IsNullOrWhiteSpace(botUser.Nickname) ? botUser.Name : botUser.Nickname);
					if (bot.Broadcast.Colors.BotNick == ServerBroadcastColor.Specific)
					{
						Discord.Color discordColor = botUser.Roles.OrderBy(r => r.Position).Last()?.Color;
						if (discordColor != null && discordColor != Discord.Color.Default)
							botNick.Color = new Color(discordColor.R, discordColor.G, discordColor.B);
					}
					else if (bot.Broadcast.Colors.BotNick == ServerBroadcastColor.Group || bot.Broadcast.Colors.BotNick == ServerBroadcastColor.Message)
						botNick.Color = colorDictionary[bot.Broadcast.Colors.BotNick.ToString()];

					var prefixes = new List<ChatMessage.Section>(e.Message.Prefixes);
					if (bot.Broadcast.Colors.Prefixes == ServerBroadcastColor.None)
						prefixes.ForEach(p => p.Color = null);
					else if (bot.Broadcast.Colors.Prefixes == ServerBroadcastColor.Group || bot.Broadcast.Colors.Prefixes == ServerBroadcastColor.Message)
						prefixes.ForEach(p => p.Color = colorDictionary[bot.Broadcast.Colors.Prefixes.ToString()]);

					ChatMessage.Section name = e.Message.Name;
					if (bot.Broadcast.Colors.Name == ServerBroadcastColor.None)
						name.Color = null;
					else if (bot.Broadcast.Colors.Name == ServerBroadcastColor.Group || bot.Broadcast.Colors.Name == ServerBroadcastColor.Message)
						name.Color = colorDictionary[bot.Broadcast.Colors.Name.ToString()];

					var suffixes = new List<ChatMessage.Section>(e.Message.Suffixes);
					if (bot.Broadcast.Colors.Suffixes == ServerBroadcastColor.None)
						suffixes.ForEach(s => s.Color = null);
					else if (bot.Broadcast.Colors.Suffixes == ServerBroadcastColor.Group || bot.Broadcast.Colors.Suffixes == ServerBroadcastColor.Message)
						suffixes.ForEach(s => s.Color = colorDictionary[bot.Broadcast.Colors.Suffixes.ToString()]);

					string text = e.Message.Text;

					#endregion

					await botUser.SendMessage(ChatHandler.CreateMessage(bot.Broadcast.Format)
						.SetHeader(botNick.ToString())
						.Prefix(prefixes)
						.SetName(name.ToString())
						.Suffix(suffixes)
						.SetText(text).ToString()
						.FormatChat(e.ChatFormatters)
						.ParseColors(colorDictionary)).LogExceptions();
				}
			}
		}

		private async void onGreet(GreetPlayerEventArgs e)
		{
			if (e.Handled)
				return;

			if (Client.State == ConnectionState.Connected)
			{
				try
				{
					TSPlayer p = TShock.Players[e.Who];
					if (p != null)
					{
						foreach (string s in Config.TerrariaChannels)
						{
							Channel c = Client.CurrentServer.FindChannels(s, exactMatch: true).FirstOrDefault();
							if (c != null)
							{
								await c.SendMessage($"`{p.Name}` has joined.");
							}
						}

						foreach (ConfigFile.ServerBot bot in Config.ServerBots.FindAll(b => b.Id > 0))
						{
							User botUser = Client.CurrentServer.GetUser(bot.Id);

							if (botUser == null || !botUser.IsBot /* Apparently bots can be "Offline" while being connected?? || botUser.Status == UserStatus.Offline*/)
							{
								// We only support active bots, mang
								Client.Log.Warning("OnGreet", $"Broadcasting to bot {bot.Id} failed (null: {botUser == null} | IsBot: {botUser?.IsBot} | Status: {botUser?.Status?.Value})");
								return;
							}

							var roleColor = Client.CurrentServer.CurrentUser.Roles.OrderBy(r => r.Position).LastOrDefault()?.Color;
							var color = roleColor == null ? Color.Yellow : new Color(roleColor.R, roleColor.G, roleColor.B);

							string name = Client.CurrentServer.CurrentUser.Nickname ?? Client.CurrentServer.CurrentUser.Name;
							await botUser.SendMessage($"{TShock.Utils.ColorTag($"{name}>", color)} {TShock.Utils.ColorTag($"{p.Name} has joined.", Color.Yellow)}").LogExceptions();
						}
					}
				}
				catch { }
			}
		}

		private void onInitialize(EventArgs args)
		{
			Config = ConfigFile.Read();
			ChatHandler.Config = Config;

			Commands.ChatCommands.Add(new TShockAPI.Command(Permissions.Use, doDiscord, "bridge", "discord"));

			Client = new BridgeClient(this);

			// Install the command service
			Client.UsingCommands(x =>
			{
				x.PrefixChar = Config.BotPrefix;
				x.HelpMode = HelpMode.Private;
				x.CustomPrefixHandler = m =>
				{
					// Do not require a prefix for private message commands (not counting bots, naturally)
					if (!m.User.IsBot && m.Channel.IsPrivate && m.Channel == m.User.PrivateChannel)
						return 0;
					else
						return -1;
				};
			});

			initDiscordCommands();

			Logins = new LoginManager(Client);
		}

		private async void onLeave(LeaveEventArgs e)
		{
			try
			{
				TSPlayer p = TShock.Players[e.Who];
				if (p != null && !String.IsNullOrWhiteSpace(p.Name))
				{
					foreach (string s in Config.TerrariaChannels)
					{
						Channel c = Client.CurrentServer.FindChannels(s, exactMatch: true).FirstOrDefault();
						if (c != null)
						{
							await c.SendMessage($"`{p.Name}` has left.");
						}
					}

					foreach (ConfigFile.ServerBot bot in Config.ServerBots.FindAll(b => b.Id > 0))
					{
						User botUser = Client.CurrentServer.GetUser(bot.Id);

						if (botUser == null || !botUser.IsBot /* Apparently bots can be "Offline" while being connected?? || botUser.Status == UserStatus.Offline*/)
						{
							// We only support active bots, mang
							Client.Log.Warning("OnLeave", $"Broadcasting to bot {bot.Id} failed (null: {botUser == null} | IsBot: {botUser?.IsBot} | Status: {botUser?.Status?.Value})");
							return;
						}

						var roleColor = Client.CurrentServer.CurrentUser.Roles.OrderBy(r => r.Position).LastOrDefault()?.Color;
						var color = roleColor == null ? Color.Yellow : new Color(roleColor.R, roleColor.G, roleColor.B);

						string name = Client.CurrentServer.CurrentUser.Nickname ?? Client.CurrentServer.CurrentUser.Name;
						await botUser.SendMessage($"{TShock.Utils.ColorTag($"{name}>", color)} {TShock.Utils.ColorTag($"{p.Name} has left.", Color.Yellow)}").LogExceptions();
					}
				}
			}
			catch { }
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
