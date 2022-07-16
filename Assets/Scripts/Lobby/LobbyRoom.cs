using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyRoom : MonoBehaviour
    , IPointerClickHandler {
    public TMPro.TextMeshProUGUI roomId;
    public TMPro.TextMeshProUGUI message;
    public TMPro.TextMeshProUGUI numPlayers;
    public GameObject inGame;
    private Color32 green = new(77, 142, 6, 255);
    private Color32 red = new(219, 28, 24, 255);
    private Room room;

    public void Init(Room room) {
        this.room = room;
        roomId.text = room.roomId.ToString();
        message.text = room.message;
        numPlayers.text = room.numPlayers.ToString() + "/4";
        if (room.numPlayers < 4 && !room.started) numPlayers.color = green;
        else numPlayers.color = red;
        if (room.started) inGame.SetActive(true);
    }

    public async void OnPointerClick(PointerEventData eventData) {
        if (room.started || room.numPlayers >= 4) return;
        Util.StartTransition();
        JoinRoom body = new();
        body.playerName = Static.playerName;
        body.roomId = room.roomId;
        var stringContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/join-room", stringContent);
        var returnBody = JsonConvert.DeserializeObject<JoinRoomReturn>(await response.Content.ReadAsStringAsync());
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Static.httpServerAddress, returnBody.port);
        ConnectionData connectionData = new();
        connectionData.passcode = returnBody.passcode;
        connectionData.playerName = Static.playerName;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(connectionData));
        NetworkManager.Singleton.StartClient();
    }

}

public class Room {
    public int roomId;
    public string message;
    public int numPlayers;
    public bool started;
}