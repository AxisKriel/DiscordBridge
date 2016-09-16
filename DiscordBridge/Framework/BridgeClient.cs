using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
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
						SetGame(new Game("Terraria", GameType.Default, "http://sbplanet.co/"));
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

			if (e.Channel.IsPrivate)
			{
				if (e.User.IsBot && _main.Config.OtherServerBots.Exists(b => b.Id == e.User.Id))
				{
					// Message is Multi-Server Broadcast
					TSPlayer.All.SendMessage(e.Message.Text, Color.White);

					// Strip tags when sending to console
					TSPlayer.Server.SendMessage(e.Message.Text.StripTags(), Color.White);
				}
				else
				{
					// Because I don't feel like making a custom handler, tell users to use the prefix
					await e.Channel.SendMessage($"Type `{_main.Config.BotPrefix}help` for a list of available commands.");
				}
			}
			else
			{
				// Ignore commands
				if (e.Message.Text.TrimStart()[0] == _main.Config.BotPrefix)
					return;

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
					string roleName = topRole.IsEveryone ? _main.Config.DefaultRoleName : topRole.Name;
					Color roleColor = topRole.IsEveryone ? Color.White : new Color(topRole.Color.R, topRole.Color.G, topRole.Color.B);

					string name = String.IsNullOrWhiteSpace(_main.Config.CustomNameFormat) ? e.User.Name
						: String.Format(_main.Config.CustomNameFormat, roleName, e.User.Name,
						String.IsNullOrWhiteSpace(e.User.Nickname) ? e.User.Name : e.User.Nickname);

					// Colorize name
					if (_main.Config.UseColoredNames)
						name = TShock.Utils.ColorTag(name, roleColor);

					string text = String.Format(_main.Config.GameChatFormat, name, e.Message.Text);

					TSPlayer.All.SendMessage(text, Color.White);

					// Strip tags when sending to console
					TSPlayer.Server.SendMessage(text.StripTags(), Color.White);
				}
			}
		}

		#endregion
	}
}
