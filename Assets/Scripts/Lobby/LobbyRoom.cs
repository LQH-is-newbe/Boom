using UnityEngine;

public class LobbyRoom : MonoBehaviour {
    [SerializeField]
    private TMPro.TextMeshProUGUI roomId;
    [SerializeField]
    private TMPro.TextMeshProUGUI message;
    [SerializeField]
    private TMPro.TextMeshProUGUI numPlayers;
    [SerializeField]
    private GameObject inGame;
    [SerializeField]
    private GameObject hasPassword;

    private Color32 green = new(77, 142, 6, 255);
    private Color32 red = new(219, 28, 24, 255);
    private Room room;
    private Lobby lobby;

    private void Awake() {
        lobby = GameObject.Find("LobbyUI").GetComponent<Lobby>();
    }

    public void Init(Room room) {
        this.room = room;
        roomId.text = room.roomId.ToString();
        message.text = room.message;
        numPlayers.text = room.numPlayers.ToString() + "/4";
        if (room.numPlayers < 5 - Static.playerNames.Count && !room.started) numPlayers.color = green;
        else numPlayers.color = red;
        inGame.SetActive(room.started);
        hasPassword.SetActive(room.hasPassword);
    }

    public void JoinRoom() {
        if (room.started || room.numPlayers >= 5 - Static.playerNames.Count) return;
        if (room.hasPassword) {
            lobby.JoinRoomWindow(room.roomId);
        } else {
            StartCoroutine(Util.JoinRoomCoroutine(room.roomId, null, (response) => {
                lobby.OpenPrompt(response);
            }));
        }
    }

}

public class Room {
    public int roomId;
    public string message;
    public int numPlayers;
    public bool started;
    public bool hasPassword;
}

public class JoinRoomReturn {
    public ushort port;
    public string passcode;
}