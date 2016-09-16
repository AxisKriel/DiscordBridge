using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TShockAPI;

namespace DiscordBridge
{
	public class ConfigFile
	{
		public const string FILENAME = "DiscordBridge.json";

		protected class Contents
		{
			public char BotPrefix = '!';
			public string BotToken;
			public string[] TerrariaChannels = new[] { "terraria" };
			public string MinimumRoleToBroadcast = "";
			public string DefaultRoleName = "Player";

			public bool StripTagsFromConsole = true;

			public string DiscordChatFormat = "**<{1}> {2}{3}:** {4}";

			public string GameChatFormat = "[c/00ffb9:Discord>] {0}[c/00ffb9::] {1}";
			public bool UseColoredNames = true;
			public bool UseTShockColors = true;
			public string CustomNameFormat = "<{0}> {2}";

			public ServerBot[] OtherServerBots = new[] { new ServerBot() };
		}

		public class ServerBot
		{
			/// <summary>
			/// The bot's application identifier.
			/// </summary>
			public ulong Id { get; set; }

			/// <summary>
			/// How to format messages before sending them to this bot:
			/// {0} - Header (set to bot nick), {1} - Prefixes, {2} - Sender, {3} - Suffixes, {4} - Message Body,
			/// {5} - Message color in hex, {6} - Header color in hex, {7} - Name color in hex
			/// </summary>
			public string OutgoingFormat { get; set; } = "{0}> <{1}> {2}{3}[c/{7}::] {4}";
		}

		protected Contents Data = new Contents();

		public char BotPrefix
		{
			get { return Data.BotPrefix; }
			set
			{
				Data.BotPrefix = value;
				save();
			}
		}

		/// <summary>
		/// The bot token used for connecting to this bot.
		/// </summary>
		public string BotToken
		{
			get { return Data.BotToken; }
			set
			{
				Data.BotToken = value;
				save();
			}
		}

		/// <summary>
		/// The list of <see cref="Discord.Channel"/>s to relay messages to/from.
		/// </summary>
		public string[] TerrariaChannels
		{
			get { return Data.TerrariaChannels; }
			set
			{
				Data.TerrariaChannels = value;
				save();
			}
		}

		/// <summary>
		/// The name that should be used for users without a role (@everyone).
		/// </summary>
		public string DefaultRoleName
		{
			get { return Data.DefaultRoleName; }
			set
			{
				Data.DefaultRoleName = value;
				save();
			}
		}

		/// <summary>
		/// Whether or not to remove all tags from chat messages when read in the console.
		/// Improves readability but prevents you from reading exactly what is being sent.
		/// </summary>
		public bool StripTagsFromConsole
		{
			get { return Data.StripTagsFromConsole; }
			set
			{
				Data.StripTagsFromConsole = value;
				save();
			}
		}

		/// <summary>
		/// Tells the bot how to format messages being sent from in-game to the Discord channel:
		/// {0} - Header, {1} - Prefixes, {2} - Sender, {3} - Suffixes, {4} - Message Body
		/// </summary>
		public string DiscordChatFormat
		{
			get { return Data.DiscordChatFormat; }
			set
			{
				Data.DiscordChatFormat = value;
				save();
			}
		}

		/// <summary>
		/// Tells the bot how to format messages before they're broadcasted to the game.
		/// {0} - Name (can be modified with <see cref="CustomNameFormat"/>);
		/// {1} - Text.
		/// </summary>
		public string GameChatFormat
		{
			get { return Data.GameChatFormat; }
			set
			{
				Data.GameChatFormat = value;
				save();
			}
		}

		/// <summary>
		/// Whether or not to colorize names based on their role.
		/// </summary>
		public bool UseColoredNames
		{
			get { return Data.UseColoredNames; }
			set
			{
				Data.UseColoredNames = value;
				save();
			}
		}

		/// <summary>
		/// If <see cref="UseColoredNames"/> is true and the user is logged in, use their group color over role.
		/// </summary>
		public bool UseTShockColors
		{
			get { return Data.UseTShockColors; }
			set
			{
				Data.UseTShockColors = value;
				save();
			}
		}

		/// <summary>
		/// Modifies how the name parameter works in <see cref="GameChatFormat"/>.
		/// If not set, defaults to "Name".
		/// {0} - User role;
		/// {1} - User name.
		/// {2} - User nickname (or name if non-existant) on the server.
		/// </summary>
		public string CustomNameFormat
		{
			get { return Data.CustomNameFormat; }
			set
			{
				Data.CustomNameFormat = value;
				save();
			}
		}

		/// <summary>
		/// The minimum role a user must have to have their messages broadcasted into the game.
		/// If not set, will broadcast everyone's messages.
		/// </summary>
		public string MinimumRoleToBroadcast
		{
			get { return Data.MinimumRoleToBroadcast; }
			set
			{
				Data.MinimumRoleToBroadcast = value;
				save();
			}
		}

		/// <summary>
		/// A list of other Discord Bridge bots added by the '!addbot' command.
		/// This bot will broadcast all messages received from the game to every bot on this list
		/// following their <see cref="ServerBot.OutgoingFormat"/>, as well as transmit everything
		/// received from them to the game.
		/// </summary>
		public List<ServerBot> OtherServerBots
		{
			get { return new List<ServerBot>(Data.OtherServerBots); }
			set
			{
				Data.OtherServerBots = value.ToArray();
				save();
			}
		}

		private void save()
		{
			try
			{
				string path = Path.Combine(TShock.SavePath, FILENAME);
				Task.Run(() => File.WriteAllText(path, JsonConvert.SerializeObject(Data, Formatting.Indented)));
			}
			catch (Exception e)
			{
				TShock.Log.ConsoleError(e.Message);
				TShock.Log.Error(e.ToString());
			}
		}

		public static ConfigFile Read()
		{
			ConfigFile config = new ConfigFile();

			try
			{
				string path = Path.Combine(TShock.SavePath, FILENAME);

				if (File.Exists(path))
					config.Data = JsonConvert.DeserializeObject<Contents>(File.ReadAllText(path));

				config.save();

				// Todo: stick a config read event here so that the bot may connect to missing channels, etc

				return config;
			}
			catch (Exception e)
			{
				TShock.Log.ConsoleError(e.Message);
				TShock.Log.Error(e.ToString());

				return new ConfigFile();
			}
		}
	}
}
