using UnityEngine;

public class MuteUnmute : MonoBehaviour {
    [SerializeField]
    private GameObject MuteIcon;
    [SerializeField]
    private GameObject UnmuteIcon;

    private void Update() {
        if (Static.audio == null) return;
        if (Static.audio.Mute) {
            if (!MuteIcon.activeSelf) {
                MuteIcon.SetActive(true);
                UnmuteIcon.SetActive(false);
            }
        } else {
            if (!UnmuteIcon.activeSelf) {
                MuteIcon.SetActive(false);
                UnmuteIcon.SetActive(true);
            }
        }
    }

    public void Toggle() {
        Static.audio.Mute = !Static.audio.Mute;
    }
}
