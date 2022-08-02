using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {
    public void ResumeGame() {
        gameObject.SetActive(false);
    }

    public void QuitGame() {
        if (Static.singlePlayer) {
            NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
        } else {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }
}
