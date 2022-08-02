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
        foreach (Player player in Player.players.Values) {
            if (!player.IsNPC) player.CharacterName = null;
        }
    }

    private void InitObjects() {
        GameObject ui = Instantiate(uiPrefab);
        MapSelection mapSelection = ui.GetComponent<MapSelection>();
        mapSelection.mapName.Value = new(Static.maps[0]);
        mapSelection.hasPrevious.Value = false;
        mapSelection.hasNext.Value = Static.maps.Length > 1;
        ui.GetComponent<NetworkObject>().Spawn(true);
        ui.GetComponent<CharacterSelection>().Init();
    }
}
