using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class GameStarter : MonoBehaviour {
    public GameObject mapPrefab;
    public GameObject uiPrefab;
    private void Start() {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetGameState();

        InitObjects();
    }

    private void ResetGameState() {
        Static.livingPlayers.Clear();
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            Static.livingPlayers.Add(clientId);
        }
    }

    private void InitObjects() {
        GameObject map = Instantiate(mapPrefab, Vector2.zero, Quaternion.identity);
        map.GetComponent<NetworkObject>().Spawn(true);
        map.GetComponent<MapLoader>().LoadMap();

        GameObject ui = Instantiate(uiPrefab);
        ui.GetComponent<NetworkObject>().Spawn(true);

        for (int clientIndex = 0; clientIndex < NetworkManager.Singleton.ConnectedClientsIds.Count; ++clientIndex) {
            Util.AddClientObjects(clientIndex, NetworkManager.Singleton.ConnectedClientsIds[clientIndex]);
        }
    }
}
