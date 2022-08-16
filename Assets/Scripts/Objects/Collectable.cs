using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Collectable: MapElement {
    public class Type {
        private readonly string name;
        private readonly float prob;
        private readonly bool isAttribute;
        private readonly System.Action<Character> apply;

        public Type(string name, float prob, bool isAttribute, System.Action<Character> apply) {
            this.name = name;
            this.prob = prob;
            this.isAttribute = isAttribute;
            this.apply = apply;
        }

        public float Prob { get { return prob; } }
        public string Name { get { return name; } }
        public void Apply(Character collecter) {
            if (isAttribute) {
                if (collecter.collectables.ContainsKey(this)) {
                    collecter.collectables[this]++;
                } else {
                    collecter.collectables[this] = 1;
                }
            }
            apply(collecter);
        }
    }

    private static readonly Type[] types = {
        new Type("Speed", 0.8f, true, collecter => collecter.Speed += 0.6f),
        new Type("BombPower", 1.0f, true, collecter => collecter.BombPower++),
        new Type("BombCapacity", 1.0f, true, collecter => collecter.BombCapacity++),
        new Type("Health", 0.36f, false, collecter => collecter.ChangeHealth(1))
    };

    private static readonly float createProb = 0.5f;

    public static void AssignDestroyableDrops() {
        float[] cumulativeProb = new float[types.Length];
        float probSum = 0;
        for (int i = 0; i < types.Length; ++i) {
            probSum += types[i].Prob;
            cumulativeProb[i] = probSum;
        }
        for (int i = 0; i < types.Length; ++i) {
            cumulativeProb[i] /= probSum / createProb;
        }
        int[] randPermutation = Random.RandomPermutation(Static.totalDestroyableNum, Static.totalDestroyableNum);
        for (int i = 0; i < Static.totalDestroyableNum; ++i) {
            float randFloat = (float)randPermutation[i] / Static.totalDestroyableNum;
            for (int j = 0; j < types.Length; ++j) {
                if (randFloat < cumulativeProb[j]) {
                    Static.destroyables[i].collectableType = types[j];
                    break;
                }
            }
        }
    }

    public static void CreateCharacterDeadDrops(Character character) {
        // TODO: collectable overlap with bomb or collectable
        Dictionary<Type, int> collectables = character.collectables;
        int emptyBlockCount = 0;
        for (int x = 0; x < Static.mapSize; ++x) {
            for (int y = 0; y < Static.mapSize; ++y) {
                if (Static.mapBlocks[new(x, y)].element == null) emptyBlockCount++;
            }
        }
        int countSum = 0;
        Dictionary<Type, int> cumulativeCount = new();
        foreach (Type type in collectables.Keys) {
            countSum += collectables[type];
            cumulativeCount[type] = countSum;
        }
        int[] randPermutation = Random.RandomPermutation(emptyBlockCount, emptyBlockCount);
        int i = 0;
        for (int x = 0; x < Static.mapSize; ++x) {
            for (int y = 0; y < Static.mapSize; ++y) {
                Vector2Int mapBlock = new(x, y);
                if (Static.mapBlocks[mapBlock].element == null) {
                    foreach (Type type in collectables.Keys) {
                        if (randPermutation[i] < cumulativeCount[type]) {
                            Collectable collectable = new(mapBlock, type);
                            collectable.Create(true, new(character.Position.x - 0.5f, character.Position.y - 0.5f));
                            break;
                        }
                    }
                    ++i;
                }
            }
        }
    }

    private static readonly GameObject collectablePrefab = Resources.Load<GameObject>("Collectable/Collectable");

    public Type type;

    public Collectable(Vector2Int mapBlock, Type type): base(mapBlock) {
        this.type = type;
    }

    public void Create(bool hasSource = false, Vector2 source = default) {
        Vector2 initMapBlock = hasSource ? source : new(MapBlock.x, MapBlock.y);
        GameObject collectable = Object.Instantiate(collectablePrefab, initMapBlock, Quaternion.identity);
        CollectableController controller = collectable.GetComponent<CollectableController>();
        Static.controllers[this] = controller;
        controller.spritePath.Value = new FixedString64Bytes("Collectable/Sprites/" + type.Name);
        controller.collectable = this;
        collectable.GetComponent<NetworkObject>().Spawn(true);
        if (hasSource) {
            controller.Move(new(MapBlock.x, MapBlock.y));
        } else {
            controller.TakeBlock();
        }
    }

    public void TakeBlock() {
        Static.collectables.Add(this);
        Static.mapBlocks[MapBlock].element = this;
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
