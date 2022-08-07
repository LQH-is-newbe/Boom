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
    public Explode explode;
    private bool createNext = true;
    public float TimeToExplode { get { return timer - explode.ExistTime + Static.explodeInterval; } }
    public float TimeToDestroy { get { return timer; } }

    private void Update() {
        if (!NetworkManager.Singleton.IsServer) return;
        timer -= Time.deltaTime;
        if (createNext && timer < explode.ExistTime - Static.explodeInterval) {
            explode.Extend();
            createNext = false;
        }
        if (timer < 0) {
            explode.Destroy();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!IsServer) return;
        if (other.CompareTag("Character")) {
            other.GetComponent<CharacterController>().character.ChangeHealth(-1);
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
}
