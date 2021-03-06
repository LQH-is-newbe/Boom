using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class Collectable : NetworkBehaviour {
    public class Type {
        private string name;
        private float prob;
        private System.Action<Character> apply;

        public Type(string name, float prob, System.Action<Character> apply) {
            this.name = name;
            this.prob = prob;
            this.apply = apply;
        }

        public float Prob { get { return prob; } }
        public string Name { get { return name; } }
        public void Apply(Character collecter) {
            apply(collecter);
        }
    }

    public class Creater {
        private static Type[] types = {
            new Type("Speed", 0.8f, collecter => collecter.ChangeSpeedClientRpc(0.6f)),
            new Type("BombPower", 1.0f, collecter => collecter.ChangeBombPower(1)),
            new Type("BombCapacity", 1.0f, collecter => collecter.ChangeBombCapacity(1)),
            new Type("Health", 0.36f, collecter => collecter.ChangeHealth(1))
        };
        private static float probSum;
        private static float[] cumulativeProb;
        private static float createProb = 0.4f;

        static Creater() {
            cumulativeProb = new float[types.Length];
            probSum = 0;
            for (int i = 0; i < types.Length; ++i) {
                probSum += types[i].Prob;
                cumulativeProb[i] = probSum;
            }
        }

        public static void AttempCreateCollectable(GameObject collectablePrefab, Vector2 position) {
            if (Random.RandomFloat() < createProb) {
                float rand = Random.RandomFloat() * probSum;
                for (int i = 0; i < types.Length; ++i) {
                    if (rand < cumulativeProb[i]) {
                        GameObject collectable = Instantiate(collectablePrefab, position, Quaternion.identity);
                        collectable.GetComponent<Collectable>().Init(types[i]);
                        collectable.GetComponent<NetworkObject>().Spawn(true);
                        return;
                    }
                }
            }
        }
    }

    public SpriteRenderer spriteRenderer;
    private Type type;
    public NetworkVariable<FixedString64Bytes> spritePath = new();

    private void OnTriggerEnter2D(Collider2D other) {
        if (!NetworkManager.Singleton.IsServer) return;
        if (other.GetComponent<Character>() != null) {
            type.Apply(other.GetComponent<Character>());
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn() {
        spriteRenderer.sprite = Resources.Load<Sprite>(spritePath.Value.Value);
    }

    public void Init(Type type) {
        this.type = type;
        spritePath.Value = new FixedString64Bytes("Objects/Collectable/Sprites/" + type.Name);
    }

    public void Destroy() {
        Destroy(gameObject);
    }
}
