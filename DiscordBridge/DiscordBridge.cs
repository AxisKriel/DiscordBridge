using System;
using System.Linq;
using Discord;
using DiscordBridge.Extensions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

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

		private void onInitialize(EventArgs e)
		{
			Config = ConfigFile.Read();

			Commands.ChatCommands.Add(new Command(Permissions.Use, doDiscord, "bridge", "discord"));
		}

		private async void onPostInitialize(EventArgs e)
		{
			Client = new BridgeClient(this);

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
