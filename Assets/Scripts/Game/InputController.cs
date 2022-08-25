using UnityEngine;

public class InputController : MonoBehaviour{
    [SerializeField]
    private GameObject pauseMenu;

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            pauseMenu.SetActive(true);
            if (Static.local) Util.PauseGame(true);
        }
    }
}
