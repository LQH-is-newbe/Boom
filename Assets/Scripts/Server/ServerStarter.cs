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

public class ServerStarter : MonoBehaviour {
    private void Start() {
        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        Static.httpServerAddress = "host.docker.internal";
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", 7777);
        Static.passcode = Environment.GetEnvironmentVariable("PASSCODE");
        RoomId roomId = new();
        roomId.roomId = int.Parse(Environment.GetEnvironmentVariable("ROOM_ID"));
        Static.roomIdJson = new StringContent(JsonConvert.SerializeObject(roomId), Encoding.UTF8, "application/json");
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
        var connectionData = JsonConvert.DeserializeObject<ConnectionData>(System.Text.Encoding.ASCII.GetString(request.Payload));
        if (connectionData.passcode != Static.passcode) {
            response.Approved = false;
            return;
        }
        response.Approved = true;
        var clientId = request.ClientNetworkId;
        Static.playerNames[clientId] = connectionData.playerName;
    }
    private void OnClientConnectedCallback(ulong clientId) {
        Debug.Log("client joined");
        Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-join", Static.roomIdJson);
    }

    private void OnClientDisconnectCallback(ulong clientId) {
        Debug.Log("client left");
        Static.playerCharacters.Remove(clientId);
        Static.playerNames.Remove(clientId);
        Static.livingPlayers.Remove(clientId);
        Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-leave", Static.roomIdJson);
    }
}

public class ConnectionData {
    public string playerName;
    public string passcode;
}
