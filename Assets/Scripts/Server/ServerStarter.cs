using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class ServerStarter : MonoBehaviour {
    private void Start() {
        Static.debugMode = Environment.GetEnvironmentVariable("BOOM_DEVELOPMENT") != null;
        Application.targetFrameRate = Static.targetFrameRate;

        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        if (!Static.debugMode) {
            Static.httpServerAddress = "host.docker.internal";
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", 7777);
            ushort port = ushort.Parse(Environment.GetEnvironmentVariable("PORT"));
            Static.port = port;
            Static.portStringContent = new StringContent(JsonConvert.SerializeObject(new { port }), Encoding.UTF8, "application/json");
        }
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
        var connectionData = JsonConvert.DeserializeObject<ConnectionData>(Encoding.ASCII.GetString(request.Payload));
        if (!Static.debugMode) {
            if (Client.clients.Count == 0) {
                var requestBody = new { Static.port, connectionData.passcode };
                var stringContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                HttpResponseMessage confirmResponse = Util.Sync(Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/confirm-room", stringContent));
                if (confirmResponse.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                    response.Approved = false;
                    return;
                } else {
                    Static.passcode = connectionData.passcode;
                }
            } else {
                if (connectionData.passcode != Static.passcode) {
                    response.Approved = false;
                    return;
                }
            }
        }
        response.Approved = true;
        Client client = new(request.ClientNetworkId, connectionData.playerNames);
        if (SceneManager.GetActiveScene().name == "Room") {
            foreach (int playerId in client.playerIds) {
                GameObject.Find("RoomUI").GetComponent<CharacterSelection>().AddPlayer(Player.players[playerId]);
            }
        }
    }

    private void OnClientDisconnectCallback(ulong clientId) {
        if (Client.clients.Count == 1 && !Static.debugMode) {
            Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/close-room", Static.portStringContent);
            Player.players.Clear();
            Client.clients.Clear();
            NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
            return;
        }
        Client client = Client.clients[clientId];
        client.Remove();
    }
}

public class ConnectionData {
    public List<string> playerNames;
    public string passcode;
}
