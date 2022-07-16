using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Bomb : NetworkBehaviour {
    private float timer;
    public float TimeToExplode { get { return timer; } }
    public float explodeTime = 2f;
    public BoxCollider2D bombCollider;
    public GameObject explodePrefab;
    private int bombPower;
    private Character creater;
    private int x, y;

    private void Awake() {
        timer = explodeTime;
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
        if (!NetworkManager.Singleton.IsServer) return;
        timer -= Time.deltaTime;
        if (timer < 0) Explode();
    }

    private void OnTriggerExit2D(Collider2D character) {
        Physics2D.IgnoreCollision(bombCollider, character, false);
    }

    public void Init(int x, int y, Character creater, int bombPower) {
        this.x = x;
        this.y = y;
        this.creater = creater;
        this.bombPower = bombPower;
    }

    public void Explode() {
        Destroy(gameObject);
        creater.NotifyBombExplode();
        Vector2 position = new Vector2(x + 0.5f, y + 0.5f);
        Explode explode = Instantiate(explodePrefab, position, Quaternion.identity).GetComponent<Explode>();
        explode.Init(x, y, bombPower, Direction.None);
    }


}
