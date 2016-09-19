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

		public async Task<bool> Authenticate(ulong id)
		{
			if (await ContainsData(id))
			{
				_client[id] = new BridgeUser(TShock.Users.GetUserByID(await GetData(id)), _client.CurrentServer.GetUser(id));
				return true;
			}
			else
			{
				_client[id] = new BridgeUser(_client.CurrentServer.GetUser(id));
				return false;
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

		public Task SetData(BridgeUser user)
		{
			return Task.Run(() =>
			{
				if (user.IsLoggedIn)
					File.WriteAllText(Path.Combine(DirPath, user.DiscordUser.Id.ToString()), user.User.ID.ToString());
			});
		}

		public Task RemoveData(ulong id)
		{
			return Task.Run(() => File.Delete(Path.Combine(DirPath, id.ToString())));
		}
	}
}
