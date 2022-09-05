using System;
using System.Collections.Generic;

public class PathFinding {
    public static bool SearchWayOut(float speed, Vector2Int source, float initTime, AIPrediction prediction, bool ignoreExplode) {
        Map<AIPredictionMapBlock> map = prediction.map;
        if (map[AIUtil.PosToMapBlock(source)].IsSafe(initTime)) return true;
        bool found = false;
        Search(
            speed,
            source,
            initTime,
            prediction,
            ignoreExplode,
            out _,
            isQuickSearch: true,
            OnNodePop: (node) => {
                if (!map[AIUtil.PosToMapBlock(node.pos)].IsSafe(node.time)) {
                    return false;
                }
                found = true;
                return true;
            });
        return found;
    }

    public static void Search(
        float speed,
        Vector2Int source,
        float initTime,
        AIPrediction prediction,
        bool ignoreExplode,
        out FastestNodes fastestNodes,
        bool isQuickSearch = false,
        Func<SearchNode, bool> OnNodePop = null,
        Func<SearchNode, float> Heuristic = null) {

        if (OnNodePop == null) OnNodePop = (_) => { return false; };
        if (Heuristic == null) Heuristic = (_) => { return 0; };

        Map<AIPredictionMapBlock> map = prediction.map;
        int initIntervalIndex = map[AIUtil.PosToMapBlock(source)].GetIntervalIndex(initTime);

        PriorityQueue<SearchNode> pq = new(AIUtil.allowedError);
        SearchNode init = new(source, initIntervalIndex, null, initTime, 0);
        pq.Add(init, Heuristic(init));

        fastestNodes = isQuickSearch ? new DictionaryFastestNodes(map) : new MapArrayFastestNodes(map);
        fastestNodes.Set(source, initIntervalIndex, init);

        int nodes = 0;
        while (!pq.Empty()) {
            ++nodes;
            SearchNode cur = pq.Pop();

            if (OnNodePop(cur)) goto End;

            Vector2Int curMapBlock = AIUtil.PosToMapBlock(cur.pos);
            AIPredictionMapBlock curAiMapBlock = map[curMapBlock];

            //int[] randomOrderOfDirections = Random.RandomPermutation(4, 4);
            for (int i = 0; i < 4; ++i) {
                Vector2Int nextPos = cur.pos + Direction.directions[i].Vector2Int;
                Vector2Int nextMapBlock = AIUtil.PosToMapBlock(nextPos);

                if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;

                AIPredictionMapBlock nextAiMapBlock = map[nextMapBlock];

                if (nextAiMapBlock.IsNoneDestroyable()) continue;

                float transitionTime = AIUtil.PosMapManhattanDistance(cur.pos, nextPos) / speed;

                // possible enter times, enter each interval as soon as possible (greedy)
                foreach (float startTime in nextAiMapBlock.TimesToEnter(cur.time)) {
                    float endTime = startTime + transitionTime;
                    int intervalIndex = nextAiMapBlock.GetIntervalIndex(endTime);
                    SearchNode fastestNode = fastestNodes.Get(nextPos, intervalIndex);
                    if (fastestNode != null && fastestNode.time <= endTime
                        || nextAiMapBlock.IsDestroyable(startTime)
                        || !ignoreExplode && (nextAiMapBlock.ExplodeOverlap(startTime, endTime + AIUtil.moveLagErrorTime) || curAiMapBlock.ExplodeOverlap(cur.time, endTime + AIUtil.moveLagErrorTime))
                        || nextAiMapBlock.Bomb(startTime) != null && !nextMapBlock.Equals(curMapBlock)
                        ) continue;
                    float waitTime = startTime - cur.time;
                    SearchNode node = new(nextPos, intervalIndex, cur, endTime, waitTime);
                    fastestNodes.Set(nextPos, intervalIndex, node);
                    if (fastestNode != null) pq.Remove(node);
                    pq.Add(node, endTime + Heuristic(node));
                }
            }
        }
    End:;
        //if (!isQuickSearch) Debug.Log("Nodes searched: " + nodes);
    }
}

public class SearchNode {
    public Vector2Int pos;
    public int intervalIndex;
    public SearchNode prev;
    public float time;
    public float waitTime;

    public SearchNode(Vector2Int pos, int intervalIndex, SearchNode prev = null, float time = -1, float waitTime = -1) {
        this.pos = pos;
        this.intervalIndex = intervalIndex;
        this.prev = prev;
        this.time = time;
        this.waitTime = waitTime;
    }

    public override bool Equals(object obj) {
        if (obj is SearchNode p) {
            return pos.Equals(p.pos) && intervalIndex == p.intervalIndex;
        } else {
            return false;
        }
    }

    public override int GetHashCode() {
        return (pos.x, pos.y, intervalIndex).GetHashCode();
    }

    public override string ToString() {
        return time.ToString();
    }
}

public struct PosInterval {
    private Vector2Int pos;
    private readonly int intervalIndex;

    public PosInterval(Vector2Int pos, int intervalIndex) {
        this.pos = pos;
        this.intervalIndex = intervalIndex;
    }

    public override int GetHashCode() {
        return pos.GetHashCode() ^ intervalIndex.GetHashCode();
    }

    public override bool Equals(object obj) {
        if (obj is PosInterval p) {
            return pos.Equals(p.pos) && intervalIndex == p.intervalIndex;
        } else {
            return false;
        }
    }
}

public abstract class FastestNodes {
    public abstract void Set(Vector2Int pos, int intervalIndex, SearchNode node);
    public abstract SearchNode Get(Vector2Int pos, int intervalIndex);
    public abstract int GetIntervalCount(Vector2Int pos);
}

public class MapArrayFastestNodes : FastestNodes {
    private readonly Map<SearchNode[]> fastestArriveTimes;

    public MapArrayFastestNodes(Map<AIPredictionMapBlock> aiMap) {
        fastestArriveTimes = new(Static.mapSize * 2);
        for (int x = 0; x < Static.mapSize * 2; x++) {
            for (int y = 0; y < Static.mapSize * 2; y++) {
                Vector2Int curPos = new(x, y);
                fastestArriveTimes[curPos] = new SearchNode[aiMap[AIUtil.PosToMapBlock(curPos)].IntervalCount()];
            }
        }
    }

    public override void Set(Vector2Int pos, int intervalIndex, SearchNode node) {
        fastestArriveTimes[pos][intervalIndex] = node;
    }

    public override SearchNode Get(Vector2Int pos, int intervalIndex) {
        return fastestArriveTimes[pos][intervalIndex];
    }

    public override int GetIntervalCount(Vector2Int pos) {
        return fastestArriveTimes[pos].Length;
    }

    public override string ToString() {
        string result = "";
        for (int y = Static.mapSize * 2 - 1; y >= 0; --y) {
            for (int x = 0; x < Static.mapSize * 2; ++x) {
                Vector2Int pos = new(x, y);
                result += new Vector2Int(x, y) + ":";
                for (int i = 0; i < fastestArriveTimes[pos].Length; ++i) {
                    result += Get(pos, i) + ",";
                }
                result += " ";
            }
            result += Environment.NewLine;
        }
        return result;
    }
}

public class DictionaryFastestNodes : FastestNodes {
    private readonly Map<AIPredictionMapBlock> aiMap;
    private readonly Dictionary<PosInterval, SearchNode> fastestArriveTimes = new();

    public DictionaryFastestNodes(Map<AIPredictionMapBlock> aiMap) {
        this.aiMap = aiMap;
    }

    public override void Set(Vector2Int pos, int intervalIndex, SearchNode node) {
        fastestArriveTimes[new(pos, intervalIndex)] = node;
    }

    public override SearchNode Get(Vector2Int pos, int intervalIndex) {
        if (fastestArriveTimes.TryGetValue(new(pos, intervalIndex), out SearchNode result)) {
            return result;
        } else {
            return null;
        }
    }

    public override int GetIntervalCount(Vector2Int pos) {
        return aiMap[AIUtil.PosToMapBlock(pos)].IntervalCount();
    }
}
