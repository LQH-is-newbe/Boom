using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Net.Http;

public class RoomUI : NetworkBehaviour {
    private void Awake() {
        gameObject.name = "RoomUI";
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc() {
        //TransitionClientRpc();
        if (!Static.singlePlayer) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/start-game", Static.roomIdJson);
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    //[ClientRpc]
    //public void TransitionClientRpc() {
    //    Util.StartTransition();
    //}
}
