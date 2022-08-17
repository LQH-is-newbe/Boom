using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameMessage : NetworkBehaviour {
    private TMPro.TextMeshProUGUI message;
    private Animator animator;
    private int nextCountDownNumber;

    public override void OnNetworkSpawn() {
        message = GetComponent<TMPro.TextMeshProUGUI>();
        animator = GetComponent<Animator>();
        animator.SetTrigger("CountDown");
        nextCountDownNumber = 3;
        ChangeCountDownMessage();
    }

    private void ChangeCountDownMessage() {
        if (nextCountDownNumber > 0) {
            message.text = nextCountDownNumber.ToString();
            nextCountDownNumber--;
            gameObject.AddComponent<Timer>().Init(1f, () => { ChangeCountDownMessage(); });
        } else {
            message.text = "GO!";
        }
    }

    [ClientRpc]
    public void GameOverClientRpc(string message) {
        animator.SetTrigger("GameOver");
        this.message.text = message;
    }
}
