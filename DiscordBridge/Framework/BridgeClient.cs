using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Logging;
using DiscordBridge.Chat;
using DiscordBridge.Extensions;
using TerrariaApi.Server;
using TShockAPI;

namespace DiscordBridge.Framework
{
	public class BridgeClient : DiscordClient
	{
		public const string LOG_PATH = "DiscordLogs";

		private DiscordBridge _main;

		protected Dictionary<string, StreamWriter> Writers { get; }

		/// <summary>
		/// The server to broadcast messages to. This is currently always the first server on the list,
		/// but it should be improved upon in the future by adding a config option + command for changing servers.
		/// </summary>
		public Server CurrentServer => Servers.FirstOrDefault();

		protected Dictionary<ulong, BridgeUser> Users { get; }

		internal BridgeClient()
		{
			Users = new Dictionary<ulong, BridgeUser>();
			Writers = new Dictionary<string, StreamWriter>();

			MessageReceived += onMessageReceived;
		}

		internal BridgeClient(DiscordBridge main) : this()
		{
			_main = main;

			Task.Run(() => initLog()).LogExceptions();
		}

		protected override void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
				MessageReceived -= onMessageReceived;

				foreach (var sr in Writers.Values)
					sr.Dispose();

				if (State == ConnectionState.Connected)
				{
					SetGame("");
					ExecuteAndWait(Disconnect);
				}
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="BridgeUser"/> associated with the given discord user identity.
		/// </summary>
		/// <param name="id">The discord user Id.</param>
		/// <returns>The associated Bridge player object.</returns>
		public BridgeUser this[ulong id]
		{
			get
			{
				if (!Users.ContainsKey(id))
					return null;

				return Users[id];
			}
			set
			{
				Users[id] = value;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="BridgeUser"/> associated with a given discord user.
		/// </summary>
		/// <param name="u">The discord user.</param>
		/// <returns>The associated Bridge player object.</returns>
		public BridgeUser this[User u]
		{
			get { return this[u.Id]; }
			set { Users[u.Id] = value; }
		}

		private void initLog()
		{
			string nameDateFormat = "yyyy-MM-dd_hh-mm-ss";
			string logDateFormat = "yyyy-MM-dd hh:mm:ss";
			
			Log.Message += async (o, e) =>
			{
				/* This is going to be purely a message log to keep all messages said in channels by non-bot users,
				   unless it's a Terraria channel. Source will always be channel name, and each channel will have
				   a separate folder. */
				if (!String.IsNullOrWhiteSpace(e.Source) && e.Severity == LogSeverity.Info)
				{
					Directory.CreateDirectory(Path.Combine(TShock.SavePath, LOG_PATH, e.Source));

					string filename = $"{DateTime.Now.ToString(nameDateFormat)}.log";
					string date = DateTime.Now.ToString(logDateFormat);

					if (!Writers.ContainsKey(e.Source))
					{
						Writers.Add(e.Source, new StreamWriter(
							new FileStream(Path.Combine(TShock.SavePath, LOG_PATH, e.Source, filename),
							FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true });
					}

					await Writers[e.Source].WriteLineAsync($"{date} - {e.Message}");
				}
			};
		}

		public async Task StartUp()
		{
			if (!String.IsNullOrWhiteSpace(_main.Config.BotToken))
			{
				try
				{
					await Connect(_main.Config.BotToken, TokenType.Bot);
					if (State == ConnectionState.Connected)
					{
						// Do everything that needs to be done when the bot connects to the server here

						if (CurrentGame.Name != "Terraria")
							SetGame(new Game("Terraria", GameType.Default, "https://terraria.org"));
					}
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
		}

		public async Task<bool> JoinServer(string inviteId)
		{
			Invite invite = await GetInvite(inviteId);
			if (invite == null || invite.IsRevoked)
				return false;

			return await JoinServer(invite);
		}

		public async Task<bool> JoinServer(Invite invite)
		{
			try
			{
				if (Servers.Any(s => s.Id == invite.Server.Id))
				{
					// Already joined the server
					return true;
				}

				await invite.Accept();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(ex.Message);
				TShock.Log.Error(ex.ToString());
				return false;
			}
		}

		public async Task<BridgeUser> LoadUser(User user)
		{
			BridgeUser player = this[user];

			if (player == null)
			{
				if (_main.Config.RememberLogins)
					return await _main.Logins.Authenticate(user.Id);
				else
					return this[user] = new BridgeUser(user);
			}

			return player;
		}

		#region Handlers

		private async void onMessageReceived(object sender, MessageEventArgs e)
		{
			if (e.Message.IsAuthor)
				return;

			try
			{
				// Ignore commands
				if (e.Message.IsMentioningMe() || e.Message.Text.StartsWith(_main.Config.BotPrefix.ToString()))
					return;

				// We don't need to process attachments; in fact, this crashes the server
				if (e.Message.Attachments.Length > 0 || e.Message.Embeds.Length > 0)
					return;

				if (e.Channel.IsPrivate)
				{
					if (e.User.IsBot && _main.Config.ServerBots.Exists(b => b.Id == e.User.Id))
					{
						// Message is Multi-Server Broadcast
						TSPlayer.All.SendMessage(e.Message.Text, _main.ChatHandler.ChatColorOverride ?? Color.White);

						// Strip tags when sending to console
						TSPlayer.Server.SendMessage(e.Message.Text.StripTags(), _main.ChatHandler.ChatColorOverride ?? Color.White);
					}
				}

				// We don't want to broadcast what other bots are saying; when we do, we use multi-server chat
				else if (e.User.IsBot)
				{
					// If the channel is a Terraria channel, log the message
					if (_main.Config.TerrariaChannels.Contains(e.Channel.Name))
						Log.Info(e.Channel.Name, $"{e.User.Name}> {e.Message.Text}");

					return;
				}

				else
				{
					// Only broadcast non-self messages sent in game channels
					if (_main.Config.TerrariaChannels.Contains(e.Channel.Name))
					{
						if (!String.IsNullOrWhiteSpace(_main.Config.MinimumRoleToBroadcast))
						{
							// Check for talk permission based on role
							Role role = e.Server.FindRoles(_main.Config.MinimumRoleToBroadcast, true).FirstOrDefault();

							// If role is null, incorrect setup, so disregard (should probably notify the log, however...)
							if (role != null && !e.User.Roles.Contains(role))
							{
								// If the user doesn't have the set role, check role positions
								int highestRolePosition = e.User.Roles.OrderBy(r => r.Position).Last().Position;
								if (highestRolePosition < role.Position)
								{
									// Todo: notify the user that their messages are not being broadcasted, but only nag them once
									return;
								}
							}
						}

						Role topRole = e.User.Roles.OrderBy(r => r.Position).Last();
						Color roleColor = topRole.IsEveryone ? Color.Gray : new Color(topRole.Color.R, topRole.Color.G, topRole.Color.B);

						BridgeUser player = await LoadUser(e.User);

						// Setting up the color dictionary - if there is need for more colors, they may be added later
						var colorDictionary = new Dictionary<string, Color?>
						{
							["Role"] = roleColor,
							["Group"] = player.IsLoggedIn ? new Color(player.Group.R, player.Group.G, player.Group.B) : roleColor,
						};

						#region Format Colors

						var roleName = new ChatMessage.Section(topRole.IsEveryone ? _main.Config.DefaultRoleName : topRole.Name);
						if (_main.Config.Broadcast.Colors.Role == DiscordBroadcastColor.Role || _main.Config.Broadcast.Colors.Role == DiscordBroadcastColor.Group)
							roleName.Color = colorDictionary[_main.Config.Broadcast.Colors.Role.ToString()];

						var name = new ChatMessage.Section(e.User.Name);
						if (_main.Config.Broadcast.Colors.Name == DiscordBroadcastColor.Role || _main.Config.Broadcast.Colors.Name == DiscordBroadcastColor.Group)
							name.Color = colorDictionary[_main.Config.Broadcast.Colors.Name.ToString()];

						var nick = new ChatMessage.Section(String.IsNullOrWhiteSpace(e.User.Nickname) ? e.User.Name : e.User.Nickname);
						if (_main.Config.Broadcast.Colors.Nickname == DiscordBroadcastColor.Role || _main.Config.Broadcast.Colors.Nickname == DiscordBroadcastColor.Group)
							nick.Color = colorDictionary[_main.Config.Broadcast.Colors.Nickname.ToString()];

						string text = e.Message.Text;

						#endregion

						string msg = String.Format(_main.Config.Broadcast.Format.ParseColors(colorDictionary),
							roleName, name, nick, text);

						TSPlayer.All.SendMessage(msg, _main.ChatHandler.ChatColorOverride ?? Color.White);

						// Strip tags when sending to console
						TSPlayer.Server.SendMessage(msg.StripTags(), _main.ChatHandler.ChatColorOverride ?? Color.White);
					}

					Log.Info(e.Channel.Name, $"Discord> {e.User.Name}: {e.Message.Text}");
				}
			}
			catch
			{
				/* All exceptions are caught here to avoid crashes. Task life ain't easy, heh?
				 * Seeing as the main purpose of this handler is to relay messages, it won't mind
				 * missing on one or two messages from time to time, specially when said messages
				 * are usually the irregular ones. */
			}
		}

		#endregion
	}
}
