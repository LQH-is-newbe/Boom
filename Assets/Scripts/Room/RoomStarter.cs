using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class RoomStarter : MonoBehaviour {
    public GameObject uiPrefab;

    private void Start() {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetRoomState();

        InitObjects();
    }

    private void ResetRoomState() {
        Static.playerCharacters.Clear();
    }

    private void InitObjects() {
        GameObject ui = Instantiate(uiPrefab);
        ui.GetComponent<NetworkObject>().Spawn(true);
        ui.GetComponent<CharacterSelection>().Init();
    }
}
