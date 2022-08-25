using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Collectable: MapElement {
    public class Type {
        public static readonly Type[] types = {
            new Type("Speed", 0.7f, true, collecter => collecter.Speed += 0.4f),
            new Type("BombPower", 0.8f, true, collecter => collecter.BombPower++),
            new Type("BombCapacity", 1.0f, true, collecter => collecter.BombCapacity++),
            new Type("Health", 0.3f, false, collecter => collecter.ChangeHealth(1))
        };
        private static readonly float[] cumulativeProb;

        static Type() {
            cumulativeProb = new float[types.Length];
            float probSum = 0;
            for (int i = 0; i < types.Length; ++i) {
                probSum += types[i].Prob;
                cumulativeProb[i] = probSum;
            }
            for (int i = 0; i < types.Length; ++i) {
                cumulativeProb[i] /= probSum;
            }
        }

        public static float[] CumulativeProb(float multiplier = 1) {
            if (multiplier == 1) {
                return cumulativeProb;
            } else {
                float[] result = new float[types.Length];
                for (int i = 0; i < types.Length; ++i) {
                    result[i] = cumulativeProb[i] * multiplier;
                }
                return result;
            }
        }

        private readonly bool isAttribute;
        private readonly System.Action<Character> apply;

        private Type(string name, float prob, bool isAttribute, System.Action<Character> apply) {
            Name = name;
            Prob = prob;
            this.isAttribute = isAttribute;
            this.apply = apply;
        }

        public float Prob { get; }
        public string Name { get; }
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

    public static Type[] RandomTypes(int num, float probMultiplier = 1) {
        Type[] result = new Type[num];
        int[] randPermutation = Random.RandomPermutation(num, num);
        float[] cumulativeProb = probMultiplier == 1 ? Type.CumulativeProb() : Type.CumulativeProb(probMultiplier);
        for (int i = 0; i < num; ++i) {
            float randFloat = (randPermutation[i] + Random.RandomFloat()) / num;
            for (int j = 0; j < Type.types.Length; ++j) {
                if (randFloat < cumulativeProb[j]) {
                    result[i] = Type.types[j];
                    break;
                }
            }
        }
        return result;
    }

    public static List<Collectable> AssignRandomPosition(Dictionary<Type, int> collectables) {
        // TODO: collectable overlap with bomb or collectable
        List<Collectable> result = new();
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
                            result.Add(new(mapBlock, type));
                            break;
                        }
                    }
                    ++i;
                }
            }
        }
        return result;
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
}
