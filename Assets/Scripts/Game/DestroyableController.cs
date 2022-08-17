using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DestroyableController : NetworkBehaviour {
    private const float explodeTime = 0.5f;
    private const float explodeFadeAwayTime = 0.1f;
    public Destroyable destroyable;

    public override void OnDestroy() {
        Static.hasObstacle[new((int)transform.position.x, (int)transform.position.y)] = false;
    }

    public void DestroyBlock() {
        gameObject.AddComponent<Timer>().Init(explodeTime, () => {
            destroyable.RemoveBlock();
            Destroy(gameObject);
            BlockDestroyClientRpc();
        });
        gameObject.AddComponent<Timer>().Init(explodeTime - explodeFadeAwayTime, () => { ExplodeFadeAwayClientRpc(); });
        BlockExplodeClientRpc();
    }

    [ClientRpc]
    private void ExplodeFadeAwayClientRpc() {
        AlphaGradient fadeAway = gameObject.AddComponent<AlphaGradient>();
        fadeAway.Init(true, GetComponent<SpriteRenderer>(), explodeFadeAwayTime);
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
