using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBridge.Chat;
using DiscordBridge.Extensions;
using TShockAPI;

namespace DiscordBridge.Framework
{
	public class BridgeClient : DiscordClient
	{
		private DiscordBridge _main;

		/// <summary>
		/// The server to broadcast messages to. This is currently always the first server on the list,
		/// but it should be improved upon in the future by adding a config option + command for changing servers.
		/// </summary>
		public Server CurrentServer => Servers.FirstOrDefault();

		protected Dictionary<ulong, BridgeUser> LoggedUsers { get; }

		internal BridgeClient()
		{
			LoggedUsers = new Dictionary<ulong, BridgeUser>();

			MessageReceived += onMessageReceived;
		}

		internal BridgeClient(DiscordBridge main) : this()
		{
			_main = main;
		}

		protected override void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
				MessageReceived -= onMessageReceived;

				if (State == ConnectionState.Connected)
				{
					SetGame("");
					ExecuteAndWait(Disconnect);
				}
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="BridgeUser"/> associated with a given discord user.
		/// If a user has not logged in to a TShock user, this will return <see cref="null"/>.
		/// Likewise, setting a value to <see cref="null"/> is the same as logging off an account.
		/// </summary>
		/// <param name="u">The discord user.</param>
		/// <returns>The associated Bridge player object, or the compiler default if the user is not logged in.</returns>
		public BridgeUser this[User u]
		{
			get
			{
				if (!LoggedUsers.ContainsKey(u.Id))
					return null;

				return LoggedUsers[u.Id];
			}
			set
			{
				if (value == null)
					LoggedUsers.Remove(u.Id);
				else
					LoggedUsers[u.Id] = value;
			}
		}

		public async Task StartUp()
		{
			if (!String.IsNullOrWhiteSpace(_main.Config.BotToken))
			{
				await Connect(_main.Config.BotToken, TokenType.Bot);
				if (State == ConnectionState.Connected)
				{
					// Do everything that needs to be done when the bot connects to the server here

					if (CurrentGame.Name != "Terraria")
						SetGame(new Game("Terraria", GameType.Default, "https://terraria.org"));
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
						TSPlayer.All.SendMessage(e.Message.Text, Color.White);

						// Strip tags when sending to console
						TSPlayer.Server.SendMessage(e.Message.Text.StripTags(), Color.White);
					}
					else
					{
						// Because I don't feel like making a custom handler, give some general info about tshock commands
						await e.Channel.SendMessage($"TShock commands must be prefixed with `{_main.Config.BotPrefix}do `."
							+ $"\nExample: `{_main.Config.BotPrefix}do who`.");
					}
				}

				// We don't want to broadcast what other bots are saying; when we do, we use multi-server chat
				else if (e.User.IsBot)
					return;

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

						// Setting up the color dictionary - if there is need for more colors, they may be added later
						var colorDictionary = new Dictionary<string, Color?>
						{
							["Role"] = roleColor,
							["Group"] = this[e.User] != null ? new Color(this[e.User].Group.R, this[e.User].Group.G, this[e.User].Group.B) : roleColor,
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

						TSPlayer.All.SendMessage(msg, Color.White);

						// Strip tags when sending to console
						TSPlayer.Server.SendMessage(msg.StripTags(), Color.White);
					}
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
