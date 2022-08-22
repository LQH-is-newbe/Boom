using System.Collections;
using System.Collections.Generic;
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
        string response = Util.JoinRoom(roomId, password.text);
        if (response == "Password incorrect") {
            passwordIncorrectPrompt.SetActive(true);
        } else if (response != "success") {
            Close();
            lobby.OpenPrompt(response);
        }
    }

    public void TestCanJoin() {
        canJoin = password.text.Trim() != "";
        joinButton.GetComponent<Image>().sprite = canJoin ? activeButton : inActiveButton;
    }

    public void Close() {
        gameObject.SetActive(false);
    }
}
