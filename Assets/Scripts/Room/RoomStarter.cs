using UnityEngine;
using Unity.Netcode;

public class RoomStarter : MonoBehaviour {
    public GameObject uiPrefab;

    private void Start() {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetRoomState();

        InitObjects();
    }

    private void ResetRoomState() {
        foreach (Client client in Client.clients.Values) {
            client.IsReady = false;
        }
    }

    private void InitObjects() {
        GameObject ui = Instantiate(uiPrefab);
        MapSelection mapSelection = ui.GetComponent<MapSelection>();
        mapSelection.mapName.Value = new(Static.maps[Static.mapIndex]);
        mapSelection.hasPrevious.Value = Static.maps.Length > 1 && Static.mapIndex > 0;
        mapSelection.hasNext.Value = Static.maps.Length > 1 && Static.mapIndex < Static.maps.Length - 1;
        ui.GetComponent<NetworkObject>().Spawn(true);
        ui.GetComponent<CharacterSelection>().Init();
    }
}
