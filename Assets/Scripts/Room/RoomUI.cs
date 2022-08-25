using UnityEngine.SceneManagement;
using Unity.Netcode;

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
