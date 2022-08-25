using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class SpriteLoader : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> path = new();

    public override void OnNetworkSpawn() {
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(path.Value.Value);
    }
}
