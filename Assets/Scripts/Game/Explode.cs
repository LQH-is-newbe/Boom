using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Explode : MonoBehaviour {
    private Direction direction;
    private int powerLeft;
    public GameObject explodePrefab;
    public float explodeInterval;
    public float existTime;
    private float timer;
    private bool createNext = true;
    private int x, y;

    private void Awake() {
        if (!NetworkManager.Singleton.IsServer) return;
        timer = existTime;
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    private void Update() {
        if (!NetworkManager.Singleton.IsServer) return;
        timer -= Time.deltaTime;
        if (createNext && timer < existTime - explodeInterval && powerLeft > 1) {
            if (direction == Direction.None) {
                Direction[] directions = new Direction[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down };
                foreach (Direction direction in directions) {
                    CreateExplode(direction);
                }
            } else {
                CreateExplode(direction);
            }
            createNext = false;
        }
        if (timer < 0) {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!NetworkManager.Singleton.IsServer) return;
        if (other.GetComponent<Character>() != null) {
            other.GetComponent<Character>().ChangeHealth(-1);
        } else if (other.GetComponent<Collectable>()) {
            other.GetComponent<Collectable>().Destroy();
        }
    }

    public void Init(int x, int y, int powerLeft, Direction direction) {
        this.x = x;
        this.y = y;
        this.powerLeft = powerLeft;
        this.direction = direction;

        string path = "Objects/Explode/Sprites/";
        if (direction == Direction.None) {
            path += "center";
        } else {
            if (powerLeft == 1) {
                path += "end";
            } else {
                path += "middle";
            }
            float rotateAngle = 0;
            if (direction == Direction.Left) rotateAngle = 90;
            else if (direction == Direction.Down) rotateAngle = 180;
            else if (direction == Direction.Right) rotateAngle = 270;
            transform.Rotate(0, 0, rotateAngle, Space.Self);
        }
        GetComponent<SpriteLoader>().path.Value = new FixedString64Bytes(path);
        GetComponent<NetworkObject>().Spawn(true);
    }

    private void CreateExplode(Direction direction) {
        int nx = x, ny = y;
        switch (direction) {
            case Direction.Left:
                --nx; break;
            case Direction.Right:
                ++nx; break;
            case Direction.Up:
                ++ny; break;
            case Direction.Down:
                --ny; break;
            default:
                break;
        }
        if (nx < 0 || nx >= Static.n || ny < 0 || ny > Static.n) return;
        GameObject next = Static.map[nx, ny];
        if (next == null) {
            Explode explode = Instantiate(explodePrefab, new Vector2(nx + 0.5f, ny + 0.5f), Quaternion.identity).GetComponent<Explode>();
            explode.Init(nx, ny, powerLeft - 1, direction);
        } else {
            if (next.GetComponent<Destroyable>() != null) {
                next.GetComponent<Destroyable>().Destroy();
            } else if (next.GetComponent<Bomb>() != null) {
                next.GetComponent<Bomb>().Explode();
            }
        }
    }
}
