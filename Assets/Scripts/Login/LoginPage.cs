using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginPage : MonoBehaviour {
    public TMP_InputField playerName;

    public void Login() {
        Static.playerName = playerName.text;
        SceneManager.LoadScene("Lobby");
    }

    public void Singleplayer() {
        Static.singlePlayer = true;
        Static.playerName = playerName.text;
        Static.playerNames[0] = playerName.text;
        NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) => response.Approved = true;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }
}
