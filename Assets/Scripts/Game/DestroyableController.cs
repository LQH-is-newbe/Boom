using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DestroyableController : NetworkBehaviour {
    public void DestroyBlock() {
        Destroy(gameObject);
        BlockDestroyClientRpc();
    }

    [ClientRpc]
    public void BlockDestroyClientRpc() {
        Destroy(gameObject);
    }
}
