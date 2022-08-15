using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameMessage : NetworkBehaviour {
    private TMPro.TextMeshProUGUI message;
    private Animator animator;
    private float countDownTimer;

    public override void OnNetworkSpawn() {
        message = GetComponent<TMPro.TextMeshProUGUI>();
        animator = GetComponent<Animator>();
        animator.SetTrigger("CountDown");
        message.text = "3";
        countDownTimer = 3;
    }

    private void Update() {
        if (countDownTimer > 0) {
            float prev = countDownTimer;
            countDownTimer -= Time.deltaTime;
            if (prev > 2 && countDownTimer <= 2) {
                message.text = "2";
            } else if (prev > 1 && countDownTimer <= 1) {
                message.text = "1";
            } else if (countDownTimer <= 0) {
                message.text = "GO!";
            }
        }
    }

    [ClientRpc]
    public void GameOverClientRpc(string message) {
        animator.SetTrigger("GameOver");
        this.message.text = message;
    }
}
