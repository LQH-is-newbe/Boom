using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Block : MonoBehaviour {
    public int xLen, yLen;

    private void Awake() {
        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(xLen, yLen);
        collider.offset = new Vector2(xLen / 2f, yLen / 2f);
        collider.usedByComposite = true;
        if (NetworkManager.Singleton.IsServer) {
            if (GetComponent<DestroyableController>() != null) gameObject.tag = "Destroyable";
            else gameObject.tag = "NoneDestroyable";
            int x = (int)transform.position.x;
            int y = (int)transform.position.y;
            for (int dx = 0; dx < xLen; ++dx) {
                for (int dy = 0; dy < yLen; ++dy) {
                    Static.map[new(x + dx, y + dy)] = gameObject;
                }
            }
        }
    }
}
