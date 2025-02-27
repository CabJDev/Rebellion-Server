using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;

namespace GameWebServer.Hubs
{
	public struct User
	{
		public string Name { get; set; }
		public HashSet<string> ConnectionIDs { get; set; }
	}

	public static class ClientHandler
    {
        // Dictionary<Lobby Code, Game App Connection ID>
        public static Dictionary<string, string> ConnectedApplications = new Dictionary<string, string>();
        // Dictionary<Lobby Code, Game State>
        public static Dictionary<string, bool> ApplicationGameStarted = new Dictionary<string, bool>();
		// Dictionary<Lobby Code, List<Browser Client Cookie>>
		public static Dictionary<string, List<string>> LobbyPlayerMap = new Dictionary<string, List<string>>();
        // Dictionary<Browser Client Cookie, Lobby Code>
        public static Dictionary<string, string> PlayerLobbyMap = new Dictionary<string, string>();
		// Dictionary<Browser Client Cookie, Browser Client Connection ID>
		// Cookie{sessionId=SHA256Hash(Name + Game App Connection ID),name=Browser Client Username,lobbyCode=Game App Lobby Code}
		public static Dictionary<string, HashSet<string>> Users = new Dictionary<string, HashSet<string>>();
    }

    public class ClientHub : Hub
    {
		//string gameURL = "https://localhost:7003";
		string gameURL = "http://192.168.1.71:5098";

		// Game application tasks
		// Game application requests a room id
		public async Task CreateRoom()
		{
			string lobbyCode = "";

			while (lobbyCode == "" || ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
				lobbyCode = GenerateCode();

			lock (ClientHandler.ConnectedApplications) { ClientHandler.ConnectedApplications.Add(lobbyCode, Context.ConnectionId); }

			Message lobbyCodeMessage = new Message();
			lobbyCodeMessage.Type = "CreateRoom";
			lobbyCodeMessage.Content = lobbyCode;

			lock (ClientHandler.LobbyPlayerMap) { ClientHandler.LobbyPlayerMap.Add(lobbyCode, new List<string>()); }

			await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", lobbyCodeMessage);
			lock (ClientHandler.ApplicationGameStarted) { ClientHandler.ApplicationGameStarted[lobbyCode] = true; }
			Console.WriteLine($"New lobby with code {lobbyCode} created for {Context.ConnectionId}!");
		}

		// Game application adds client browser to the game
		public async Task AddPlayer(string connectionID, string lobbyCode, string name, bool success)
		{
			if (success)
			{
				string hash = StringToSHA256(name + Context.ConnectionId);
				lock (ClientHandler.LobbyPlayerMap) { ClientHandler.LobbyPlayerMap[lobbyCode].Add(hash); }
				lock (ClientHandler.PlayerLobbyMap) { ClientHandler.PlayerLobbyMap.Add(hash, lobbyCode); }
				lock (ClientHandler.Users)
				{
					HashSet<string> connections = new HashSet<string>();
					connections.Add(connectionID);
					ClientHandler.Users.Add(hash, connections);
				}

				await Clients.Client(connectionID).SendAsync("SetCookie", $"sessionId={hash},name={name},lobbyCode={lobbyCode};");
				await Clients.Client(connectionID).SendAsync("Redirect", $"{gameURL}/InLobby");
			}

			else
			{
				await Clients.Client(connectionID).SendAsync("ErrorMessage", "Unable to join!");
			}
		}

		// Game application closes connection with web server
		public async Task CloseConnection(string msg)
        {
            if (msg != "") Console.WriteLine($"{Context.ConnectionId} closed connection with error:\n {msg}");

            string disconnectedLobby = "";

            List<string> playerCookies = new List<string>();

            foreach (string lobbyCode in ClientHandler.ConnectedApplications.Keys)
                if (ClientHandler.ConnectedApplications[lobbyCode] == Context.ConnectionId)
                {
					playerCookies = ClientHandler.LobbyPlayerMap[lobbyCode];
                    lock (ClientHandler.ConnectedApplications) { ClientHandler.ConnectedApplications.Remove(lobbyCode); }
					lock (ClientHandler.ApplicationGameStarted) { ClientHandler.ApplicationGameStarted.Remove(lobbyCode); }
                    disconnectedLobby = lobbyCode;
                    break;
                }

            foreach (string playerCookie in playerCookies) 
            { 
				foreach (string connectionId in ClientHandler.Users[playerCookie])
				{
					await Clients.Client(connectionId).SendAsync("Redirect", $"{gameURL}");
				}
                lock (ClientHandler.Users) { ClientHandler.Users.Remove(playerCookie); }
                lock (ClientHandler.PlayerLobbyMap) { ClientHandler.PlayerLobbyMap.Remove(playerCookie); } 
            }

            lock (ClientHandler.LobbyPlayerMap) { ClientHandler.LobbyPlayerMap.Remove(disconnectedLobby); }

            Console.WriteLine("Removed lobby for " + Context.ConnectionId);
        }
		// Game application tasks

		// Client browser tasks
		// Client browser connects to the website
		public async Task UserConnect(string cookie)
		{
			// 0 = hash, 1 = name, 2 = lobbyCode
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Length < 3) return;

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;expires=");
				return;
			}
			string lobbyConnectionID = ClientHandler.ConnectedApplications[lobbyCode];

			if (VerifyCookieHash(hash, name, lobbyCode))
			{
				lock (ClientHandler.Users)
				{
					HashSet<string> connections = new HashSet<string>();
					if (!ClientHandler.Users.ContainsKey(hash))
					{
						connections.Add(Context.ConnectionId);
						ClientHandler.Users.Add(hash, connections);
					}
					else
					{
						connections = ClientHandler.Users[hash];
						connections.Add(Context.ConnectionId);
						ClientHandler.Users[hash] = connections;
					}
				}

				if (ClientHandler.ApplicationGameStarted[lobbyCode])
				{
					Console.WriteLine("Redirect player to the appropriate screen");
				}
				else
				{
					foreach (string connectionId in ClientHandler.Users[hash])
					{
						await Clients.Client(connectionId).SendAsync("Redirect", $"{gameURL}/InLobby");
					}
				}
			}
			else await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;expires=");
		}

		// Client browser disconnects from the website
		public async Task UserDisconnect(string cookie)
        {
            string[] cookieInfo = ParseCookie(cookie);

            if (cookieInfo.Count() < 3) return;

            string hash = cookieInfo[0];
            string name = cookieInfo[1];
            string lobbyCode = cookieInfo[2];

            if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
            {
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;expires=");
				return;
            }
            if (!VerifyCookieHash(hash, name, lobbyCode)) return;

			if (ClientHandler.Users.ContainsKey(hash))
			{
                lock (ClientHandler.Users)
                {
                    HashSet<string> connections = ClientHandler.Users[hash];
                    connections.Remove(Context.ConnectionId);
                    ClientHandler.Users[hash] = connections;
                }

				await Task.Delay(5000);

				if (ClientHandler.Users.ContainsKey(hash) && ClientHandler.Users[hash].Count == 0)
				{
					lock (ClientHandler.LobbyPlayerMap) { ClientHandler.LobbyPlayerMap[lobbyCode].Remove(hash); }
					lock (ClientHandler.PlayerLobbyMap) { ClientHandler.PlayerLobbyMap.Remove(hash); }
					lock (ClientHandler.Users) { ClientHandler.Users.Remove(hash); }

					Message msg = new Message();
					msg.Type = "RemovePlayer";
					msg.Content = $"{hash}";

					await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
				}
			}
        }

        // Client browser joins a lobby
        public async Task JoinLobby(string name, string lobbyCode)
        {
            if (ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
            {
				string hash = StringToSHA256(name + ClientHandler.ConnectedApplications[lobbyCode]);
                if (!ClientHandler.PlayerLobbyMap.ContainsKey(hash))
                {
                    Message msg = new Message();
                    msg.Type = "AddPlayer";
                    msg.Content = $"{Context.ConnectionId},{name},{hash}";

                    await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
                }
                else await Clients.Client(Context.ConnectionId).SendAsync("ErrorMessage", "Already connected!");
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorMessage", "Invalid lobby code!");
            }
        }
        // Client browser tasks

        private string GenerateCode()
        {
            Random rand = new Random();

            int codeLength = 6;
            string lobbyCode = "";

            for (int i = 0; i < codeLength; ++i)
                lobbyCode += Convert.ToChar(rand.Next(0, 26) + 65);

            return lobbyCode;
        }

		private static string StringToSHA256(string str)
		{
			byte[] byteArray = SHA256.HashData(Encoding.UTF8.GetBytes(str));
			return Convert.ToHexString(byteArray);
		}

        private string[] ParseCookie(string cookie)
        {
			string[] cookieInfo = cookie.Split(',');

			if (cookieInfo.Length < 3) return [];

			string hash = cookieInfo[0].Split('=')[1];
			string name = cookieInfo[1].Split('=')[1];
			string lobbyCode = cookieInfo[2].Split('=')[1];

            return new string[] { hash, name, lobbyCode };
		}

        private bool VerifyCookieHash(string hash, string name, string lobbyCode)
        {
            string lobbyConnectionId = ClientHandler.ConnectedApplications[lobbyCode];
            return hash == StringToSHA256(name + lobbyConnectionId);
        }
	}

    public class Message
    {
        private string type = "";
        private string content = "";

        public string Type { get => type; set => type = value; }
        public string Content { get => content; set => content = value; }
    }
}
