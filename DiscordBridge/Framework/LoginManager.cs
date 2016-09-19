using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace DiscordBridge.Framework
{
	public class LoginManager
	{
		private BridgeClient _client;

		public const string DIRECTORY = "DiscordAuth";

		public string DirPath => Path.Combine(TShock.SavePath, DIRECTORY);

		public LoginManager(BridgeClient client)
		{
			Directory.CreateDirectory(DirPath);
			
			_client = client;
		}

		public async Task<BridgeUser> Authenticate(ulong id)
		{
			if (await ContainsData(id))
			{
				return new BridgeUser(TShock.Users.GetUserByID(await GetData(id)), _client.CurrentServer.GetUser(id));
			}
			else
			{
				return new BridgeUser(_client.CurrentServer.GetUser(id));
			}
		}

		public Task<bool> ContainsData(ulong id)
		{
			return Task.Run(() =>
			{
				return File.Exists(Path.Combine(DirPath, id.ToString()));
			});
		}

		public Task<int> GetData(ulong id)
		{
			return Task.Run(() =>
			{
				if (!File.Exists(Path.Combine(DirPath, id.ToString())))
					return -1;
				else
					return Convert.ToInt32(File.ReadAllText(Path.Combine(DirPath, id.ToString())));
			});
		}

		public Task SetData(Discord.User discordUser, TShockAPI.DB.User tshockUser)
		{
			return Task.Run(() => File.WriteAllText(Path.Combine(DirPath, discordUser.Id.ToString()), tshockUser.ID.ToString()));
		}

		public Task RemoveData(ulong id)
		{
			return Task.Run(() => File.Delete(Path.Combine(DirPath, id.ToString())));
		}
	}
}
