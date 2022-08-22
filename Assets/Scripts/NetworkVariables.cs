using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkVariables : NetworkBehaviour {
    public NetworkVariable<bool> gameRunning = new();

    public override void OnNetworkSpawn() {
        Static.networkVariables = this;
    }

    [ClientRpc]
    public void ShowGameOverMessageClientRpc(string message) {
        GameObject gameOverMessage = GameObject.Find("GameOverMessage");
        gameOverMessage.GetComponent<GameOverMessage>().Init(message);
    }
}
