using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoom : MonoBehaviour {
    [SerializeField]
    private TMP_InputField password;
    [SerializeField]
    private GameObject joinButton;
    [SerializeField]
    private GameObject passwordIncorrectPrompt;
    [SerializeField]
    private Sprite activeButton;
    [SerializeField]
    private Sprite inActiveButton;

    public int roomId;
    private bool canJoin = false;
    private Lobby lobby;

    private void Awake() {
        lobby = GameObject.Find("LobbyUI").GetComponent<Lobby>();
    }

    public void Join() {
        if (!canJoin) return;
        StartCoroutine(Util.JoinRoomCoroutine(roomId, password.text, (response) => {
            if (response == "Password incorrect") {
                passwordIncorrectPrompt.SetActive(true);
            } else {
                Close();
                lobby.OpenPrompt(response);
            }
        }));
    }

    public void TestCanJoin() {
        canJoin = password.text.Trim() != "";
        joinButton.GetComponent<Image>().sprite = canJoin ? activeButton : inActiveButton;
    }

    public void Close() {
        gameObject.SetActive(false);
    }
}
