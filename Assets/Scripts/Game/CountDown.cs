using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountDown : MonoBehaviour {
    [SerializeField]
    private TMPro.TextMeshProUGUI message;

    private Color color;
    private int nextCountDownNumber;

    private void Awake() {
        color = message.color;
        nextCountDownNumber = 3;
        ChangeCountDownMessage();
    }

    private void ChangeCountDownMessage() {
        if (nextCountDownNumber > 0) {
            message.text = nextCountDownNumber.ToString();
            nextCountDownNumber--;
            gameObject.AddComponent<AlphaGradient>().Init(true, message.gameObject, 1);
            gameObject.AddComponent<Timer>().Init(1f, () => { ChangeCountDownMessage(); });
        } else {
            message.text = "GO!";
            message.color = color;
            gameObject.AddComponent<Timer>().Init(1f, () => {
                gameObject.AddComponent<AlphaGradient>().Init(true, gameObject, 0.5f);
                gameObject.AddComponent<AlphaGradient>().Init(true, message.gameObject, 0.5f);
            });
            gameObject.AddComponent<Timer>().Init(1.5f, () => { Destroy(gameObject); });
        }
    }
}
