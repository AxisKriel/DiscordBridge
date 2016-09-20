using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TShockAPI;

namespace DiscordBridge
{
	public enum DiscordBroadcastColor
	{
		None,
		Role,
		Group
	}

	public enum ServerBroadcastColor
	{
		None,
		Specific,
		Group,
		Message
	}

	public class ConfigFile
	{
		public const string FILENAME = "DiscordBridge.json";

		protected class Contents
		{
			public char BotPrefix { get; set; } = '!';
			public string BotToken { get; set; } = "";
			public string[] TerrariaChannels { get; set; } = new[] { "terraria" };
			public string MinimumRoleToBroadcast { get; set; } = "";
			public string DefaultRoleName { get; set; } = "Player";

			public bool RememberLogins { get; set; } = true;

			public bool StripTagsFromConsole { get; set; } = true;

			public string ChatColorOverride { get; set; }

			public string DiscordChatFormat { get; set; } = "**<{1}> {2}{3}:** {4}";

			public DiscordBroadcast DiscordBroadcast { get; set; } = new DiscordBroadcast
			{
				Format = "[c/00ffb9:Discord>] <{0}> {2}[c/Name::] {3}",
				Colors = new DiscordBroadcast.ColorGroup
				{
					Role = DiscordBroadcastColor.Role,
					Name = DiscordBroadcastColor.Role,
					Nickname = DiscordBroadcastColor.Role
				}
			};

			public ServerBot[] ServerBots { get; set; } = new[]
			{
				new ServerBot
				{
					Id = 0,
					Broadcast = new ServerBroadcast
					{
						Format = "[c/00ff00:{0}>] <{1}> {2}{3}[c/Name::] {4}",
						Colors = new ServerBroadcast.ColorGroup
						{
							BotNick = ServerBroadcastColor.None,
							Prefixes = ServerBroadcastColor.Group,
							Name = ServerBroadcastColor.Group,
							Suffixes = ServerBroadcastColor.Group
						}
					}
				}
			};
		}

		public class DiscordBroadcast
		{
			public struct ColorGroup
			{
				[JsonConverter(typeof(StringEnumConverter))]
				public DiscordBroadcastColor Role { get; set; }

				[JsonConverter(typeof(StringEnumConverter))]
				public DiscordBroadcastColor Name { get; set; }

				[JsonConverter(typeof(StringEnumConverter))]
				public DiscordBroadcastColor Nickname { get; set; }
			}

			public string Format { get; set; }

			public ColorGroup Colors { get; set; }
		}

		public class ServerBroadcast
		{
			public struct ColorGroup
			{
				[JsonConverter(typeof(StringEnumConverter))]
				public ServerBroadcastColor BotNick { get; set; }

				[JsonConverter(typeof(StringEnumConverter))]
				public ServerBroadcastColor Prefixes { get; set; }

				[JsonConverter(typeof(StringEnumConverter))]
				public ServerBroadcastColor Name { get; set; }

				[JsonConverter(typeof(StringEnumConverter))]
				public ServerBroadcastColor Suffixes { get; set; }
			}

			public string Format { get; set; }

			public ColorGroup Colors { get; set; }
		}

		public class ServerBot
		{
			/// <summary>
			/// The bot's application identifier.
			/// </summary>
			public ulong Id { get; set; }

			public ServerBroadcast Broadcast { get; set; }
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
		public string[] TerrariaChannels => Data.TerrariaChannels;

		/// <summary>
		/// The name that should be used for users without a role (@everyone).
		/// </summary>
		public string DefaultRoleName => Data.DefaultRoleName;

		/// <summary>
		/// Whether or not to remember logged in users between sessions.
		/// </summary>
		public bool RememberLogins => Data.RememberLogins;

		/// <summary>
		/// Whether or not to remove all tags from chat messages when read in the console.
		/// Improves readability but prevents you from reading exactly what is being sent.
		/// </summary>
		public bool StripTagsFromConsole => Data.StripTagsFromConsole;

		/// <summary>
		/// If set, will always send messages with this RGB color instead of the actual message color.
		/// </summary>
		public string ChatColorOverride => Data.ChatColorOverride;

		/// <summary>
		/// Tells the bot how to format messages being sent from in-game to the Discord channel:
		/// {0} - Header, {1} - Prefixes, {2} - Sender, {3} - Suffixes, {4} - Message Body
		/// </summary>
		public string DiscordChatFormat => Data.DiscordChatFormat;

		/// <summary>
		/// Tells the bot how to format messages before they're broadcasted to the game:
		/// {0} - Role,
		/// {1} - Name,
		/// {2} - Nickname (or name if the user doesn't have one),
		/// {3} - Text
		/// </summary>
		public DiscordBroadcast Broadcast => Data.DiscordBroadcast;

		/// <summary>
		/// The minimum role a user must have to have their messages broadcasted into the game.
		/// If not set, will broadcast everyone's messages.
		/// </summary>
		public string MinimumRoleToBroadcast => Data.MinimumRoleToBroadcast;

		/// <summary>
		/// A list of other Discord Bridge bots added by the '!addbot' command.
		/// This bot will broadcast all messages received from the game to every bot on this list
		/// following their <see cref="ServerBot.Broadcast"/> settings, as well as transmit everything
		/// received from them to the game.
		/// </summary>
		public List<ServerBot> ServerBots => new List<ServerBot>(Data.ServerBots);

		/// <summary>
		/// Adds a server bot to the broadcast list if it doesn't already contain it.
		/// </summary>
		/// <param name="botId">The bot identifier.</param>
		public bool AddBot(ulong botId)
		{
			if (!ServerBots.Exists(b => b.Id == botId))
			{
				var oldList = new List<ServerBot>(Data.ServerBots);
				oldList.Add(new ServerBot { Id = botId });
				Data.ServerBots = oldList.ToArray();
				save();
				return true;
			}

			return false;
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
