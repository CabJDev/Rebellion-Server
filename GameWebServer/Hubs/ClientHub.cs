using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Net;
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
		string gameURL = "https://rebelliongame.fun";

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
			lock (ClientHandler.ApplicationGameStarted) { ClientHandler.ApplicationGameStarted[lobbyCode] = false; }
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

		// Game application tells web server that the game has started
		public async Task GameStarted(string lobbyCode)
		{
			lock (ClientHandler.ApplicationGameStarted) { ClientHandler.ApplicationGameStarted[lobbyCode] = true; }

			foreach (string playerHash in ClientHandler.LobbyPlayerMap[lobbyCode])
			{
				if (!ClientHandler.Users.ContainsKey(playerHash)) continue;
				foreach (string connectionID in ClientHandler.Users[playerHash])
				{
					await Clients.Client(connectionID).SendAsync("Redirect", $"{gameURL}/Gameplay");
				}
			}
		}

		// Game application sends a player's role information to client browser
		public async Task SendRoleInfo(string hash, string roleName, string roleDesc, string winConditionDesc)
		{
			if (!ClientHandler.Users.ContainsKey(hash)) return;
			foreach (string connectionID in ClientHandler.Users[hash])
				await Clients.Client(connectionID).SendAsync("GetRoles", roleName, roleDesc, winConditionDesc);
		}

		// Game application sends a list of player's names to client browser
		public async Task GetNames(string hash, string[] names, int[] specials)
		{
			if (!ClientHandler.Users.ContainsKey(hash)) return;
			foreach (string connectionID in ClientHandler.Users[hash])
				await Clients.Client(connectionID).SendAsync("GetNames", names, specials);
		}

		// Game applications sends client browser a list of buttons to enable
		public async Task EnableButtons(string hash, int[] toEnable)
		{
			if (!ClientHandler.Users.ContainsKey(hash)) return;
			foreach (string connectionID in ClientHandler.Users[hash])
				await Clients.Client(connectionID).SendAsync("EnableButtons", toEnable);
		}

		// Game applications sends client browser a command to disable buttons
		public async Task DisableButtons(string lobbyCode)
		{
			foreach (string hash in ClientHandler.LobbyPlayerMap[lobbyCode])
				foreach (string connectionID in ClientHandler.Users[hash])
					await Clients.Client(connectionID).SendAsync("DisableButtons");
		} 

		// Game applications sends client browser a command to strike out a dead player
		public async Task PlayerKilled(string lobbyCode, int playerIndex)
		{
			foreach (string hash in ClientHandler.LobbyPlayerMap[lobbyCode])
				foreach (string connectionID in ClientHandler.Users[hash])
					await Clients.Client(connectionID).SendAsync("PlayerKilled", playerIndex);
		}

		// Game application sends client browsers a message in their chats
		public async Task SystemMessage(string lobbyCode, string message, string timeSent)
		{
			foreach (string hash in ClientHandler.LobbyPlayerMap[lobbyCode])
				foreach (string connectionID in ClientHandler.Users[hash])
					await Clients.Client(connectionID).SendAsync("ReceiveSystemMessage", message, timeSent);
		}

		// Game application sends client browsers a message from other players
		public async Task PlayerMessage(string sender, string[] hashes, string message)
		{
			foreach (string hash in hashes)
				foreach (string connectionID in ClientHandler.Users[hash])
					await Clients.Client(connectionID).SendAsync("ReceivePlayerMessage", sender, message);
		}

		public async Task PlayerSystemMessage(string hash, string message)
		{
			foreach (string connectionID in ClientHandler.Users[hash])
				await Clients.Client(connectionID).SendAsync("ReceiveSystemMessage", message);
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
		}
		// Game application tasks

		// Client browser tasks
		// Client browser connects to the website
		public async Task UserConnect(string cookie, string currentUrl)
		{
			// 0 = hash, 1 = name, 2 = lobbyCode
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Length < 3)
			{
				if (currentUrl != (gameURL + "/"))
					await Clients.Client(Context.ConnectionId).SendAsync("Redirect", $"{gameURL}");
				return;
			}

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
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
					foreach (string connectionId in ClientHandler.Users[hash])
					{
						await Clients.Client(connectionId).SendAsync("Redirect", $"{gameURL}/Gameplay");
					}
				}
				else
				{
					foreach (string connectionId in ClientHandler.Users[hash])
					{
						await Clients.Client(connectionId).SendAsync("Redirect", $"{gameURL}/InLobby");
					}
				}
			}
			else await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
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
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
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

					if (name.Length < 2 || name.Length > 12)
					{
						await Clients.Client(Context.ConnectionId).SendAsync("ErrorMessage", "Your name must be between 2 and 12 characters long!");
						return;
					}

					if (!name.All(char.IsLetterOrDigit))
					{
						await Clients.Client(Context.ConnectionId).SendAsync("ErrorMessage", "Your name must be alphanumeric!");
						return;
					}

					Message msg = new Message();
					msg.Type = "AddPlayer";
					msg.Content = $"{Context.ConnectionId},{name},{hash}";

					await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
				}
				else await Clients.Client(Context.ConnectionId).SendAsync("ErrorMessage", "That name already exists on that lobby!");
			}
			else
			{
				await Clients.Client(Context.ConnectionId).SendAsync("ErrorMessage", "Invalid lobby code!");
			}
		}

		// Client browser backs out of lobby
		public async Task UserIntentionalDisconnect(string cookie)
		{
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Count() < 3) return;

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
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

		// Client browser asks for player names
		public async Task RetrievePlayerNames(string cookie, long timeStamp)
		{
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Count() < 3) return;

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
				return;
			}
			if (!VerifyCookieHash(hash, name, lobbyCode)) return;

			Message msg = new Message();
			msg.Type = "RetrievePlayerNames";
			msg.Content = $"{hash},{timeStamp}";

			await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
		}

		// Client browser asks for role information
		public async Task RetrieveRoleInfo(string cookie, long timeStamp)
		{
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Count() < 3) return;

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
				return;
			}
			if (!VerifyCookieHash(hash, name, lobbyCode)) return;

			Message msg = new Message();
			msg.Type = "RetrieveRoleInfo";
			msg.Content = $"{hash},{timeStamp}";

			await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
		}

		// Client browser sends target selected
		public async Task PlayerTarget(string cookie, int target, long timeStamp)
		{
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Count() < 3) return;

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
				return;
			}
			if (!VerifyCookieHash(hash, name, lobbyCode)) return;

			Message msg = new Message();
			msg.Type = "SetPlayerTarget";
			msg.Content = $"{hash},{target},{timeStamp}";

			await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
		}

		// Client browser sends a message
		public async Task SendMessage(string cookie, string message)
		{
			string[] cookieInfo = ParseCookie(cookie);

			if (cookieInfo.Count() < 3) return;

			string hash = cookieInfo[0];
			string name = cookieInfo[1];
			string lobbyCode = cookieInfo[2];

			if (!ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
			{
				await Clients.Client(Context.ConnectionId).SendAsync("SetCookie", $"sessionId=,name=,lobbyCode=;expires=Thu, 01 Jan 1970 00:00:00 UTC;");
				return;
			}
			if (!VerifyCookieHash(hash, name, lobbyCode)) return;

			Message msg = new Message();
			msg.Type = "PlayerSendMessage";
			msg.Content = $"{hash},{message}";

			await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
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