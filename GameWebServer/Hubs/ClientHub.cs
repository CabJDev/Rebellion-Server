using Microsoft.AspNetCore.SignalR;

namespace GameWebServer.Hubs
{
    public static class ClientHandler
    {
        public static Dictionary<string, string> ConnectedApplications = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> LobbyPlayerMap = new Dictionary<string, List<string>>();
    }

    public class ClientHub : Hub
    {
        public Task CloseConnection(string msg)
        {
            if (msg != "") Console.WriteLine($"{Context.ConnectionId} closed connection with error:\n {msg}");

            string disconnectedLobby = "";

            foreach (string lobbyCode in ClientHandler.ConnectedApplications.Keys)
                if (ClientHandler.ConnectedApplications[lobbyCode] == Context.ConnectionId)
                {
                    ClientHandler.ConnectedApplications.Remove(lobbyCode);
                    disconnectedLobby = lobbyCode;
                    break;
                }

            ClientHandler.LobbyPlayerMap.Remove(disconnectedLobby);

            Console.WriteLine("Removed lobby for " + Context.ConnectionId);

            return Task.CompletedTask;
        }

        public Task AddPlayer(string connectionID, string lobbyCode, bool success)
        {
            if (success)
            {
                ClientHandler.LobbyPlayerMap[lobbyCode].Add(connectionID);
                Console.WriteLine($"Added {connectionID} to {lobbyCode} hosted by {Context.ConnectionId}!");
            }

            return Task.CompletedTask;
        }

        public async Task JoinLobby(string name, string lobbyCode)
        {
            if (ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
            {
                Message msg = new Message();
                msg.Type = "AddPlayer";
                msg.Content = $"{Context.ConnectionId},{name}";

                await Clients.Client(ClientHandler.ConnectedApplications[lobbyCode]).SendAsync("ReceiveMessage", msg);
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("InvalidLobby");
            }
        }

        public async Task CreateRoom()
        {
            string lobbyCode = "";

            while (lobbyCode == "" || ClientHandler.ConnectedApplications.ContainsKey(lobbyCode))
                lobbyCode = GenerateCode();

            ClientHandler.ConnectedApplications.Add(lobbyCode, Context.ConnectionId);

            Message lobbyCodeMessage = new Message();
            lobbyCodeMessage.Type = "CreateRoom";
            lobbyCodeMessage.Content = lobbyCode;

            ClientHandler.LobbyPlayerMap.Add(lobbyCode, new List<string>());

            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", lobbyCodeMessage);
            Console.WriteLine($"New lobby with code {lobbyCode} created for {Context.ConnectionId}!");
        }

        private string GenerateCode()
        {
            Random rand = new Random();

            int codeLength = 6;
            string lobbyCode = "";

            for (int i = 0; i < codeLength; ++i)
                lobbyCode += Convert.ToChar(rand.Next(0, 26) + 65);

            return lobbyCode;
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
