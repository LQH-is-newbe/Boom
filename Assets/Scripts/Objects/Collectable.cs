using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Collectable: MapElement {
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
            new Type("Speed", 0.8f, collecter => collecter.Speed += 0.6f),
            new Type("BombPower", 1.0f, collecter => collecter.BombPower++),
            new Type("BombCapacity", 1.0f, collecter => collecter.BombCapacity++),
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

        public static Type RandomCollectable() {
            if (Random.RandomFloat() < createProb) {
                float rand = Random.RandomFloat() * probSum;
                for (int i = 0; i < types.Length; ++i) {
                    if (rand < cumulativeProb[i]) {
                        return types[i];
                    }
                }
            }
            return null;
        }
    }

    private static GameObject collectablePrefab = Resources.Load<GameObject>("Collectable/Collectable");

    public Type type;

    public Collectable(Vector2Int mapBlock, Type type): base(mapBlock) {
        this.type = type;
    }

    public void Create() {
        Static.mapBlocks[MapBlock].element = this;
        GameObject collectable = Object.Instantiate(collectablePrefab, new(MapBlock.x, MapBlock.y), Quaternion.identity);
        CollectableController controller = collectable.GetComponent<CollectableController>();
        Static.controllers[this] = controller;
        controller.spritePath.Value = new FixedString64Bytes("Collectable/Sprites/" + type.Name);
        controller.collectable = this;
        collectable.GetComponent<NetworkObject>().Spawn(true);
        Static.collectables.Add(this);
    }

    public void Destroy() {
        Object.Destroy(((CollectableController)Static.controllers[this]).gameObject);
        Static.collectables.Remove(this);
        Static.controllers.Remove(this);
        Static.mapBlocks[MapBlock].element = null;
    }

    public void DestroyPrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> events, float time) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        predictionMapBlock.DestroyCollectable(time);
    }
}
