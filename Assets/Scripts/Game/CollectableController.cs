using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class CollectableController : NetworkBehaviour {
    private const float moveSpeed = 10;

    public SpriteRenderer spriteRenderer;
    public NetworkVariable<FixedString64Bytes> spritePath = new();
    public Collectable collectable;
    private Vector2 targetMapPos;
    private bool moving;

    private void Update() {
        if (!IsServer) return;
        if (moving) {
            float moveDistance = moveSpeed * Time.deltaTime;
            Vector2 diff = targetMapPos - (Vector2)transform.position;
            if (diff.magnitude > moveDistance) {
                transform.position = transform.position + (Vector3)(diff.normalized * moveDistance);
            } else {
                transform.position = targetMapPos;
                moving = false;
                TakeBlock();
            }
        }
    }

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

    public void Move(Vector2 targetMapPos) {
        this.targetMapPos = targetMapPos;
        moving = true;
    }

    public void TakeBlock() {
        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.9f);
        collider.offset = new Vector2(0.5f, 0.5f);
        collider.isTrigger = true;
        collectable.TakeBlock();
    }
}
