using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DestroyableController : NetworkBehaviour {
    private const float explodeTime = 0.5f;
    private const float explodeFadeAwayTime = 0.1f;
    private bool isExplodeFadingAway = false;
    private float explodeTimer;
    public Destroyable destroyable;

    private void Update() {
        if (!IsServer) return;
        if (explodeTimer > 0) {
            explodeTimer -= Time.deltaTime;
            if (!isExplodeFadingAway && explodeTimer < explodeFadeAwayTime) {
                ExplodeFadeAwayClientRpc();
                isExplodeFadingAway = true;
            }
            if (explodeTimer <= 0) {
                destroyable.RemoveBlock();
                Destroy(gameObject);
                BlockDestroyClientRpc();
            }
        }
    }

    public void DestroyBlock() {
        explodeTimer = explodeTime;
        BlockExplodeClientRpc();
    }

    [ClientRpc]
    private void ExplodeFadeAwayClientRpc() {
        FadeAway fadeAway = gameObject.AddComponent<FadeAway>();
        fadeAway.Init(GetComponent<SpriteRenderer>(), explodeFadeAwayTime);
    }

    [ClientRpc]
    private void BlockExplodeClientRpc() {
        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Destroyable.explodeSprite;
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void BlockDestroyClientRpc() {
        Destroy(gameObject);
    }
}
