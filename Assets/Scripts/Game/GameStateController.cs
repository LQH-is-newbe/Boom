using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameStateController : MonoBehaviour {
    public const float gameOverMessageShowTime = 2;
    private float gameOverMessagetimer;
    private float countDownTimer;

    private void Awake() {
        countDownTimer = 3;
    }

    public void TestPlayerWins() {
        if (Character.characters.Keys.Count == 1) {
            foreach (int id in Character.characters.Keys) {
                ShowGameOverMessage(Player.players[id].Name + " Wins!");
            }
        } else if (Character.characters.Keys.Count == 0) {
            ShowGameOverMessage("Game Over");
        }
    }

    private void Update() {
        if (countDownTimer > 0) {
            countDownTimer -= Time.deltaTime;
            if (countDownTimer <= 0) {
                Static.networkVariables.gameRunning.Value = true;
            }
        }
        if (gameOverMessagetimer > 0) {
            gameOverMessagetimer -= Time.deltaTime;
            if (gameOverMessagetimer <= 0) {
                NewGame();
            }
        }
    }

    public void NewGame() {
        if (!Static.singlePlayer) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/end-game", Static.roomIdJson);
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    private void ShowGameOverMessage(string message) {
        GameObject.Find("GameMessage").GetComponent<GameMessage>().GameOverClientRpc(message);
        Static.networkVariables.gameRunning.Value = false;
        gameOverMessagetimer = gameOverMessageShowTime;
    }
}
