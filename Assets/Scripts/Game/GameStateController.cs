using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameStateController : MonoBehaviour {
    public float messageShowTime = 2;
    private float messageShowTimer = 0;

    public void TestPlayerWins() {
        if (Player.livingPlayers.Count == 1) {
            ShowMessage(Player.livingPlayers[0].Name + " Wins!");
            messageShowTimer = messageShowTime;
        } else if (Player.livingPlayers.Count == 0) {
            ShowMessage("Game Over");
            messageShowTimer = messageShowTime;
        }
    }

    private void Update() {
        if (messageShowTimer > 0) {
            messageShowTimer -= Time.deltaTime;
            if (messageShowTimer < 0) NewGame();
        }
    }

    public void NewGame() {
        if (!Static.singlePlayer) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/end-game", Static.roomIdJson);
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    private void ShowMessage(string message) {
        GameMessage gameMessage = GameObject.Find("GameUI").GetComponent<GameMessage>();
        gameMessage.ShowMessageClientRpc(message);
    }
}
