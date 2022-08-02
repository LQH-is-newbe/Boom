using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BombController : NetworkBehaviour {
    private float timer;
    public float TimeToExplode { get { return timer; } }
    public BoxCollider2D bombCollider;
    public GameObject explodePrefab;
    private Character creater;
    private Bomb bomb;
    public Bomb Bomb { get { return bomb; } }

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
        if (timer < 0) Explode();
    }

    private void OnTriggerExit2D(Collider2D character) {
        Physics2D.IgnoreCollision(bombCollider, character, false);
    }

    public void Init(Character creater, int bombPower) {
        this.creater = creater;
        Vector2Int mapPos = new((int)transform.position.x, (int)transform.position.y);
        Static.map.Set(mapPos, gameObject);
        bomb = new(mapPos, bombPower);
    }

    public void Explode() {
        Static.map.Set(bomb.MapPos, null);
        Destroy(gameObject);
        creater.BombNum--;
        GameObject explode = Instantiate(explodePrefab, transform.position, Quaternion.identity);
        explode.GetComponent<ExplodeController>().Init(bomb.BombPower, Direction.None);
        explode.GetComponent<NetworkObject>().Spawn();
    }
}
