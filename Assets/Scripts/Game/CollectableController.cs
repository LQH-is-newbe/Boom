using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class CollectableController : NetworkBehaviour {
    public SpriteRenderer spriteRenderer;
    public NetworkVariable<FixedString64Bytes> spritePath = new();
    public Collectable collectable;

    private void OnTriggerEnter2D(Collider2D other) {
        if (!IsServer) return;
        if (other.GetComponent<CharacterController>() != null) {
            collectable.type.Apply(other.GetComponent<CharacterController>().character);
            collectable.Destroy();
        }
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            gameObject.tag = "Collectable";
        }
        if (IsClient) {
            spriteRenderer.sprite = Resources.Load<Sprite>(spritePath.Value.Value);
        }
    }
}
