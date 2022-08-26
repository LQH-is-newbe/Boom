using UnityEngine;

public class GameUI : MonoBehaviour {
    [SerializeField]
    private GameObject pauseMenu;

    private void Awake() {
        gameObject.name = "GameUI";
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Tab) && Static.networkVariables.gameRunning.Value) {
            bool pause = !Static.paused;
            pauseMenu.SetActive(pause);
            if (Static.local) Util.PauseGame(pause);
        }
    }
}
