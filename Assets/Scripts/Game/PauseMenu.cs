using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {
    [SerializeField]
    private GameObject howToPlay;

    public void ResumeGame() {
        gameObject.SetActive(false);
        if (Static.local) Util.PauseGame(false);
    }

    public void QuitGame() {
        Static.audio.ChangeBackgroundMusic("Lobby");
        if (Static.local) {
            Util.PauseGame(false);
            NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
        } else {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Lobby");
        }
    }

    public void SwitchHowToPlay() {
        howToPlay.SetActive(!howToPlay.activeSelf);
    }
}
