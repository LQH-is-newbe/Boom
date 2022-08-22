using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Net.Http;

public class RoomUI : NetworkBehaviour {
    private void Awake() {
        gameObject.name = "RoomUI";
    }

    public void Quit() {
        NetworkManager.Singleton.Shutdown();
        if (Static.local) SceneManager.LoadScene("Login");
        else SceneManager.LoadScene("Lobby");
    }
}
