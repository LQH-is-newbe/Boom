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
        Static.debugMode = Environment.GetEnvironmentVariable("BOOM_DEVELOPMENT") != null;
        Application.targetFrameRate = 200;

        NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        if (!Static.debugMode) {
            Static.httpServerAddress = "host.docker.internal";
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", 7777);
            Static.passcode = Environment.GetEnvironmentVariable("PASSCODE");
            RoomId roomId = new();
            roomId.roomId = int.Parse(Environment.GetEnvironmentVariable("ROOM_ID"));
            Static.roomIdJson = new StringContent(JsonConvert.SerializeObject(roomId), Encoding.UTF8, "application/json");
        }
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
        string playerName;
        if (!Static.debugMode) {
            var connectionData = JsonConvert.DeserializeObject<ConnectionData>(Encoding.ASCII.GetString(request.Payload));
            if (connectionData.passcode != Static.passcode) {
                response.Approved = false;
                return;
            }
            playerName = connectionData.playerName;
        } else {
            playerName = Random.RandomInt(100).ToString();
        }
        response.Approved = true;
        Player player = Player.CreatePlayer(false, request.ClientNetworkId, playerName);
        Debug.Log("client joined name " + player.Name);
        if (SceneManager.GetActiveScene().name == "Room") {
            GameObject.Find("RoomUI").GetComponent<CharacterSelection>().AddPlayer(player);
        }
    }

    private void OnClientDisconnectCallback(ulong clientId) {
        Player player = Player.clientPlayers[clientId];
        Debug.Log("client left name " + player.Name);
        player.Remove();
        if (SceneManager.GetActiveScene().name == "Room") {
            GameObject.Find("RoomUI").GetComponent<CharacterSelection>().RemovePlayer(player);
        }
    }
}

public class ConnectionData {
    public string playerName;
    public string passcode;
}
