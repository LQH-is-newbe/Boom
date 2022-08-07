using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BombController : NetworkBehaviour {
    private float timer;
    public float TimeToExplode { get { return timer; } }
    public BoxCollider2D bombCollider;
    public Bomb bomb;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            gameObject.tag = "Bomb";
            timer = Bomb.explodeTime;
        }
        Vector2 position = transform.position;
        Collider2D[] charactersIn = Physics2D.OverlapAreaAll(
            position,
            new Vector2(position.x + 1, position.y + 1),
            LayerMask.GetMask("Character"));
        foreach (Collider2D character in charactersIn) {
            Physics2D.IgnoreCollision(bombCollider, character);
        }
    }

    private void Update() {
        if (!IsServer) return;
        timer -= Time.deltaTime;
        if (timer < 0) {
            bomb.Trigger();
        }
    }

    private void OnTriggerExit2D(Collider2D character) {
        Physics2D.IgnoreCollision(bombCollider, character, false);
    }

    public void Destroy() {
        Destroy(gameObject);
    }
}
