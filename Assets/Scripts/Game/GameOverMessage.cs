using UnityEngine;

public class GameOverMessage : MonoBehaviour {
    [SerializeField]
    private TMPro.TextMeshProUGUI text;

    public void Init(string message) {
        gameObject.AddComponent<AlphaGradient>().Init(false, gameObject, 1);
        gameObject.AddComponent<AlphaGradient>().Init(false, text.gameObject, 1);
        text.text = message;
    }
}
