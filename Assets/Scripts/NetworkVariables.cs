using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkVariables : NetworkBehaviour {
    public NetworkVariable<bool> gameRunning = new();

    public override void OnNetworkSpawn() {
        Static.networkVariables = this;
    }

    //public void ChangeHasObstacle(int x, int y, bool value) {
    //    Static.hasObstacle[new(x, y)] = value;
    //}

    //[ClientRpc]
    //public void ChangeHasObstacleClientRpc(int x, int y, bool value) {
    //    Static.hasObstacle[new(x, y)] = value;
    //}
}
