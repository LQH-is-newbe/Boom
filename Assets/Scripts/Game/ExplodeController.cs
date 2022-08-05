using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class ExplodeController : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> spritePath = new();
    public NetworkVariable<float> rotateAngle = new();
    public GameObject explodePrefab;
    public GameObject display;
    private float timer = 10;
    private Explode explode;
    private bool createNext = true;
    public float TimeToExplode { get { return timer - explode.ExistTime + Static.explodeInterval; } }
    public float TimeToDestroy { get { return timer; } }
    public Explode Explode { get { return explode; } }

    private void Update() {
        if (!NetworkManager.Singleton.IsServer) return;
        timer -= Time.deltaTime;
        if (createNext && timer < explode.ExistTime - Static.explodeInterval) {
            explode.CreateNextExplode(OnExplodeCreate);
            createNext = false;
        }
        if (timer < 0) {
            Static.map[explode.MapPos] = null;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!IsServer) return;
        if (other.CompareTag("Character")) {
            other.GetComponent<Character>().ChangeHealth(-1);
        } else if (other.CompareTag("Collectable")) {
            other.GetComponent<Collectable>().Destroy();
        }
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            timer = explode.ExistTime;
            string path = "Explode/Sprites/";
            if (explode.Direction == Direction.None) {
                path += "center";
            } else {
                if (explode.PowerLeft == 1) {
                    path += "end";
                } else {
                    path += "middle";
                }
                rotateAngle.Value = 0;
                if (explode.Direction == Direction.Left) rotateAngle.Value = 90f;
                else if (explode.Direction == Direction.Down) rotateAngle.Value = 180f;
                else if (explode.Direction == Direction.Right) rotateAngle.Value = 270f;
            }
            spritePath.Value = new FixedString64Bytes(path);
        }
        if (IsClient) {
            display.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(spritePath.Value.Value);
            display.transform.rotation = Quaternion.identity;
            display.transform.Rotate(0, 0, rotateAngle.Value, Space.Self);
        }
    }

    public void Init(int powerLeft, Direction direction) {
        Vector2Int mapPos = new((int)transform.position.x, (int)transform.position.y);
        Static.map[mapPos] = gameObject;
        explode = new Explode(mapPos, powerLeft, direction);
    }

    private void OnExplodeCreate(Vector2Int pos, int powerLeft, Direction direction) {
        GameObject next = Static.map[pos];

        if (next == null) {
            NewExplode();
        } else {
            if (next.CompareTag("Destroyable")) {
                next.GetComponent<Destroyable>().DestroyBlock();
            } else if (next.CompareTag("Bomb")) {
                next.GetComponent<BombController>().Explode();
            } else if (next.CompareTag("NoneDestroyable")){
            } else {
                NewExplode();
            }
        }

        void NewExplode() {
            GameObject explode = Instantiate(explodePrefab, new Vector2(pos.x, pos.y), Quaternion.identity);
            explode.GetComponent<ExplodeController>().Init(powerLeft, direction);
            explode.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
