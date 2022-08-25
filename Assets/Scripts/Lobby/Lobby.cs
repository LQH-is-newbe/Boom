using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json;

public class Lobby : MonoBehaviour {
    [SerializeField]
    private GameObject lobbyRoomPrefab;
    [SerializeField]
    private GameObject lobbyRooms;
    [SerializeField]
    private GameObject createRoomWindow;
    [SerializeField]
    private GameObject joinRoomWindow;
    [SerializeField]
    private GameObject p2Display;
    [SerializeField]
    private TextMeshProUGUI p1Name;
    [SerializeField]
    private TextMeshProUGUI p2Name;
    [SerializeField]
    private GameObject prompt;
    [SerializeField]
    private TextMeshProUGUI promptText;

    private void Awake() {
        Static.httpServerAddress = Static.serverPublicAddress;
        if (Static.debugMode) Static.httpServerAddress = "127.0.0.1";
        p1Name.text = Static.playerNames[0];
        if (Static.playerNames.Count < 2) p2Display.SetActive(false);
        else p2Name.text = Static.playerNames[1];
        GetRooms();
    }

    class RoomList {
        public List<Room> rooms;
    }

    public void GetRooms() {
        foreach (Transform child in lobbyRooms.transform) {
            Destroy(child.gameObject);
        }
        StartCoroutine(Util.WebRequestCoroutine(Util.WebRequest("http://" + Static.httpServerAddress + "/get-rooms"), (statusCode, responseBody) => {
            Debug.Log(responseBody);
            List<Room> rooms = JsonConvert.DeserializeObject<RoomList>(responseBody).rooms;
            foreach (var room in rooms) {
                GameObject lobbyRoom = Instantiate(lobbyRoomPrefab);
                lobbyRoom.GetComponent<LobbyRoom>().Init(room);
                lobbyRoom.transform.SetParent(lobbyRooms.transform, false);
            }
        }));
    }

    public void CreateRoomWindow() {
        createRoomWindow.SetActive(true);
    }

    public void ReturnLogin() {
        SceneManager.LoadScene("Login");
    }

    public void JoinRoomWindow(int roomId) {
        joinRoomWindow.SetActive(true);
        joinRoomWindow.GetComponent<JoinRoom>().roomId = roomId;
    }

    public void OpenPrompt(string content) {
        prompt.SetActive(true);
        promptText.text = content;
    }

    public void ClosePrompt() {
        prompt.SetActive(false);
    }
}


