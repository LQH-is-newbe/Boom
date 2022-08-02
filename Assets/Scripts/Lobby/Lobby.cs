using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System;

public class Lobby : MonoBehaviour {
    public TMP_InputField roomMessage;
    public GameObject lobbyRoomPrefab;
    public GameObject lobbyRooms;
    public GameObject createRoomWindow;
    private bool creatingRoom = false;

    private void Awake() {
        Static.httpServerAddress = Static.serverPublicAddress;
        if (Debug.isDebugBuild) Static.httpServerAddress = "127.0.0.1";
        GetRooms();
    }

    public async void GetRooms() {
        foreach (Transform child in lobbyRooms.transform) {
            Destroy(child.gameObject);
        }
        var response = await Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/get-rooms", new StringContent(""));
        var returnBody = JsonConvert.DeserializeObject<List<Room>>(await response.Content.ReadAsStringAsync());
        foreach (var room in returnBody) {
            GameObject lobbyRoom = Instantiate(lobbyRoomPrefab);
            lobbyRoom.GetComponent<LobbyRoom>().Init(room);
            lobbyRoom.transform.SetParent(lobbyRooms.transform, false);
        }
    }

    public async void CreateRoom() {
        if (creatingRoom) return;
        Util.StartTransition();
        CreateRoom body = new();
        body.playerName = Static.playerName;
        body.message = roomMessage.text;
        var stringContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/create-room", stringContent);
        var returnBody = JsonConvert.DeserializeObject<JoinRoomReturn>(await response.Content.ReadAsStringAsync());
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Static.httpServerAddress, returnBody.port);
        ConnectionData connectionData = new();
        connectionData.passcode = returnBody.passcode;
        connectionData.playerName = Static.playerName;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(connectionData));
        NetworkManager.Singleton.StartClient();
    }

    public void CreateRoomWindow() {
        createRoomWindow.SetActive(!createRoomWindow.activeInHierarchy);
    }

    public void QuitGame() {
        Application.Quit();
    }
}

public class JoinRoom {
    public string playerName;
    public int roomId;
}

public class JoinRoomReturn {
    public ushort port;
    public string passcode;
}

public class CreateRoom {
    public string playerName;
    public string message;
}


