using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Destroyable : NetworkBehaviour {
    private Vector2Int mapBlock;
    public Vector2Int MapBlock { get { return mapBlock; } }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;
        mapBlock = new((int)transform.position.x, (int)transform.position.y);
        Static.destroyables.Add(this);
        Static.totalDestroyableNum++;
    }

    public void DestroyBlock() {
        Static.destroyables.Remove(this);
        Destroy(gameObject);
        BlockDestroyClientRpc();
        Static.map.Set(mapBlock, null);
        Vector2 position = transform.position;
        GameObject collectablePrefab = Resources.Load<GameObject>("Collectable/Collectable");
        Collectable.Creater.AttempCreateCollectable(collectablePrefab, position);
    }

    [ClientRpc]
    public void BlockDestroyClientRpc() {
        Destroy(gameObject);
    }
}
