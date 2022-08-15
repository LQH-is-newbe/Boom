using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkVariables : NetworkBehaviour {
    public NetworkVariable<bool> gameRunning = new();
}
