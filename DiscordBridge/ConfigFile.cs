using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using TShockAPI;

namespace DiscordBridge
{
	public class ConfigFile
	{
		public const string FILENAME = "DiscordBridge.json";

		protected class Contents
		{
			public string BotToken;
			public string[] TerrariaChannels = new[] { "terraria" };
			public string MinimumRoleToBroadcast = "";

			public string DiscordChatFormat = "[c/00ffb9:Discord @] {0}[c/00ffb9::] {1}";
			public bool UseColoredNames = true;
			public string CustomNameFormat = "<{0}> {1}";
		}

		protected Contents Data = new Contents();

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
		/// Tells the bot how to format messages before they're broadcasted.
		/// {0} - Name (can be modified with <see cref="CustomNameFormat"/>);
		/// {1} - Text.
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
		/// Modifies how the name parameter works in <see cref="DiscordChatFormat"/>.
		/// If not set, defaults to "Name".
		/// {0} - User role;
		/// {1} - User name.
		/// {2} - User nickname on the server.
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
