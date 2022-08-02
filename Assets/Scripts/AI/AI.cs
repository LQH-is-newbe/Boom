using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AI {
    private const float preventDis = 0.03f;
    private const float preventTime = 0.01f;
    private const float decideTime = 0.1f;

    private Character character;
    private int waitingDecisionId = -1;
    private BotController botController;

    public static Vector2Int PosToMapBlock(Vector2Int pos) {
        int x = pos.x >= 0 ? pos.x / 2 : -1;
        int y = pos.y >= 0 ? pos.y / 2 : -1;
        return new Vector2Int(x, y);
    }

    public static Vector2 PosToMapPos(Vector2Int pos) {
        Vector2Int mapBlock = PosToMapBlock(pos);
        float mapPosX = mapBlock.x;
        if (pos.x % 2 == 0) mapPosX += preventDis + Static.characterColliderSize.x / 2;
        else mapPosX += 1 - preventDis - Static.characterColliderSize.x / 2;
        float mapPosY = mapBlock.y;
        if (pos.y % 2 == 0) mapPosY += preventDis + Static.characterColliderSize.y / 2;
        else mapPosY += 1 - preventDis - Static.characterColliderSize.y / 2;
        return new Vector2(mapPosX, mapPosY);
    }

    public static float PosMapManhattanDistance(Vector2Int a, Vector2Int b) {
        Vector2 aMapPos = PosToMapPos(a);
        Vector2 bMapPos = PosToMapPos(b);
        float result = MathF.Abs(aMapPos.x - bMapPos.x) + Mathf.Abs(aMapPos.y - bMapPos.y);
        return result;
    }

    public AI(Character character, BotController botController) {
        this.character = character;
        this.botController = botController;
    }

    private string InstructionsToString(List<Instruction> instructions) {
        string path = "";
        for (int i = 0; i < instructions.Count; ++i) {
            path += instructions[i] + " ";
        }
        return path;
    }

    private List<Instruction> GetInstructions(SearchNode node, float initTime, StrategyType strategyType) {
        // TODO: node is null
        List<Instruction> instructions = new();
        SearchNode cur = node;
        if (strategyType == StrategyType.Destroyable) {
            instructions.Add(new(cur.pos, cur.time - initTime, putBomb: true));
        }
        int moveCount = 0;
        while (cur.prev != null) {
            if (moveCount++ < 8) instructions.Add(new(cur.pos, cur.time, putBomb: true));
            instructions.Add(new(cur.pos, cur.time - initTime));
            if (cur.waitTime > 0) {
                instructions.Add(new(cur.prev.pos, cur.prev.time - initTime + cur.waitTime, waitTime: cur.waitTime));
            }
            cur = cur.prev;
        }
        List<Instruction> result;
        if (decideTime == -1) {
            result = instructions;
        } else {
            result = new();
            int i = instructions.Count - 1;
            float time = 0;
            while (i >= 0 && instructions[i].time < decideTime) {
                time = instructions[i].time;
                result.Add(instructions[i--]);
            }
            if (i >= 0) {
                if (instructions[i].waitTime > 0) {
                    Instruction waitRemain = new(instructions[i].pos, decideTime, waitTime: decideTime - time);
                    result.Add(waitRemain);
                } else {
                    result.Add(instructions[i]);
                }
            } else {
                Instruction waitRemain = result.Count > 0 ? 
                    new(result[result.Count - 1].pos, decideTime, waitTime: decideTime - result[result.Count - 1].time) :
                    new(node.pos, decideTime, waitTime: decideTime);
                result.Add(waitRemain);
            }
        }
        //Debug.Log(InstructionsToString(result));

        return result;
    }

    private bool SearchWayOut(Vector2Int source, float initTime, Map<AIMapBlock> aiMap) {
        if (aiMap.Get(PosToMapBlock(source)).Empty(initTime)) return true;
        bool found = false;
        Search(
            source,
            initTime,
            aiMap,
            out _,
            isQuickSearch: true,
            OnNodePop: (node) => { 
                if (aiMap.Get(PosToMapBlock(node.pos)).Empty(node.time)) {
                    found = true;
                    return true;
                }
                return false;
            });
        return found;
    }

    private void Search(
        Vector2Int source,
        float initTime,
        Map<AIMapBlock> aiMap,
        out FastestNodes fastestNodes,
        bool isQuickSearch = false,
        Func<SearchNode, bool> OnNodePop = null, 
        Func<SearchNode, float> Heuristic = null) {

        if (OnNodePop == null) OnNodePop = (_) => { return false; };
        if (Heuristic == null) Heuristic = (_) => { return 0; };

        int initIntervalIndex = aiMap.Get(PosToMapBlock(source)).GetIntervalIndex(initTime);

        PriorityQueue<SearchNode, float> pq = new();
        SearchNode init = new(source, initIntervalIndex, null, initTime, 0);
        pq.Add(init, Heuristic(init));

        fastestNodes = isQuickSearch ? new DictionaryFastestNodes(aiMap) : new MapArrayFastestNodes(aiMap);
        fastestNodes.Set(source, initIntervalIndex, init);
        
        while (!pq.Empty()) {
            SearchNode cur = pq.Pop();

            if (OnNodePop(cur)) return;

            Vector2Int curMapBlock = PosToMapBlock(cur.pos);
            AIMapBlock curAiMapBlock = aiMap.Get(curMapBlock);

            for (int i = 0; i < 4; ++i) {
                Vector2Int nextPos = cur.pos + Static.directions[i];
                Vector2Int nextMapBlock = PosToMapBlock(nextPos);

                if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;

                AIMapBlock nextAiMapBlock = aiMap.Get(nextMapBlock);

                if (nextAiMapBlock.isNoneDestroyable) continue;

                float transitionTime = PosMapManhattanDistance(cur.pos, nextPos) / character.Speed;

                // possible enter times, enter each interval as soon as possible (greedy)
                List<float> timesToEnter = nextAiMapBlock.TimesToEnter(cur.time);
                for (int j = 0; j < timesToEnter.Count; ++j) {
                    float startTime = timesToEnter[j] + preventTime;
                    float endTime = startTime + transitionTime;
                    int intervalIndex = nextAiMapBlock.GetIntervalIndex(endTime);
                    SearchNode fastestNode = fastestNodes.Get(nextPos, intervalIndex);
                    if (fastestNode != null && fastestNode.time <= endTime
                        || nextAiMapBlock.IsDestroyable(startTime)
                        || nextAiMapBlock.ExplodeOverlap(startTime, endTime) 
                        || curAiMapBlock.ExplodeOverlap(cur.time, endTime) 
                        || nextAiMapBlock.IsBomb(startTime) && !nextMapBlock.Equals(curMapBlock)
                        ) continue;
                    float waitTime = j == 0 ? 0 : startTime - cur.time;
                    SearchNode node = new(nextPos, intervalIndex, cur, endTime, waitTime);
                    fastestNodes.Set(nextPos, intervalIndex, node);
                    if (fastestNode != null) pq.Remove(node);
                    pq.Add(node, endTime + Heuristic(node));
                }
            }
        }
    }

    public void Redecide(Vector2Int pos, float timeConsumed) {
        botController.currentInstructions = new();
        float waitTime = decideTime - timeConsumed;
        botController.currentInstructions.Add(new(pos, waitTime, waitTime: waitTime));
        Decide(pos, waitTime, AIMap.Generate(character.Id));
    }

    public Task Decide(Vector2Int source, float initTime, Map<AIMapBlock> aiMap) {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        float decisionId = ++waitingDecisionId;

        SearchNode closestDestroyableNeighbor = null;
        SearchNode closestCollectable = null;
        SearchNode closestEmpty = null;
        SearchNode closestCharacter = null;
        Func<SearchNode, bool> OnNodePop = (node) => {
            Vector2Int curMapBlock = PosToMapBlock(node.pos);
            AIMapBlock aIMapBlock = aiMap.Get(curMapBlock);
            if (closestDestroyableNeighbor == null) {
                for (int i = 0; i < 4; ++i) {
                    Vector2Int nextPos = node.pos + Static.directions[i];
                    Vector2Int nextMapBlock = PosToMapBlock(nextPos);
                    if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;
                    if (aiMap.Get(nextMapBlock).IsDestroyable(node.time + Bomb.explodeTime + Explode.explodeInterval)
                        && SearchWayOut(node.pos, node.time, aiMap)) {
                        closestDestroyableNeighbor = node;
                    }
                }
            }
            if (closestCollectable == null) {
                if (aIMapBlock.isCollectable
                    && SearchWayOut(node.pos, node.time, aiMap)) {
                    closestCollectable = node;
                }
            }
            if (closestEmpty == null && aIMapBlock.Empty(node.time)) {
                closestEmpty = node;
            }
            if (closestCharacter == null && aIMapBlock.character != null && aIMapBlock.character.Id != character.Id) {
                closestCharacter = node;
            }
            return false;
        };

        FastestNodes fastestNodes;
        Search(source, initTime, aiMap, out fastestNodes, OnNodePop: OnNodePop);

        float destroyableExistRatioThreshold = 0.5f;
        float destroyableExistRatio = Static.destroyables.Count / (float)Static.totalDestroyableNum;

        SearchNode targetNode = null;
        float targetDestroyableScore = closestDestroyableNeighbor != null && character.BombNum < character.BombCapacity ? destroyableExistRatio / destroyableExistRatioThreshold  - closestDestroyableNeighbor.time : -100;
        float targetCollectableScore = closestCollectable != null ? 6 - closestCollectable.time : -100;
        float targetCharacterScore = closestCharacter != null ? destroyableExistRatio * destroyableExistRatioThreshold - closestCharacter.time : -100;

        //Debug.Log("des:" + targetDestroyableScore + ",collec:" + targetCollectableScore + ",char:" + targetCharacterScore);
        StrategyType strategyType;
        if (targetDestroyableScore > targetCollectableScore && targetDestroyableScore > targetCharacterScore) {
            targetNode = closestDestroyableNeighbor;
            strategyType = StrategyType.Destroyable;
        } else if (targetCollectableScore > targetDestroyableScore && targetCollectableScore > targetCharacterScore) {
            targetNode = closestCollectable;
            strategyType = StrategyType.Collectable;
        } else {
            targetNode = closestCharacter;
            strategyType = StrategyType.Character;
        }

        List<Instruction> instructions;
        if (targetNode == null) {
            instructions = new();
            instructions.Add(new(source, decideTime, waitTime: decideTime));
        } else {
            instructions = GetInstructions(targetNode, initTime, strategyType);
        }

        stopwatch.Stop();
        //Debug.Log("Decide time:" + stopwatch.ElapsedMilliseconds);
        if (decisionId != waitingDecisionId) return Task.CompletedTask;
        botController.nextInstructions = instructions;
        return Task.CompletedTask;
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
        if (obj is SearchNode) {
            SearchNode p = (SearchNode)obj;
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

public enum StrategyType {
    Destroyable,
    Collectable,
    Character
}

public struct PosInterval {
    private Vector2Int pos;
    private int intervalIndex;

    public PosInterval(Vector2Int pos, int intervalIndex) {
        this.pos = pos;
        this.intervalIndex = intervalIndex;
    }

    public override int GetHashCode() {
        return pos.GetHashCode() ^ intervalIndex.GetHashCode();
    }

    public override bool Equals(object obj) {
        if (obj is PosInterval) {
            PosInterval p = (PosInterval)obj;
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
    private Map<SearchNode[]> fastestArriveTimes;

    public MapArrayFastestNodes(Map<AIMapBlock> aiMap) {
        fastestArriveTimes = new(Static.mapSize * 2);
        for (int x = 0; x < Static.mapSize * 2; x++) {
            for (int y = 0; y < Static.mapSize * 2; y++) {
                Vector2Int curPos = new Vector2Int(x, y);
                fastestArriveTimes.Set(curPos, new SearchNode[aiMap.Get(AI.PosToMapBlock(curPos)).IntervalCount()]);
            }
        }
    }

    public override void Set(Vector2Int pos, int intervalIndex, SearchNode node) {
        fastestArriveTimes.Get(pos)[intervalIndex] = node;
    }

    public override SearchNode Get(Vector2Int pos, int intervalIndex) {
        return fastestArriveTimes.Get(pos)[intervalIndex];
    }

    public override int GetIntervalCount(Vector2Int pos) {
        return fastestArriveTimes.Get(pos).Length;
    }

    public override string ToString() {
        string result = "";
        for (int y = Static.mapSize * 2 - 1; y >= 0; --y) {
            for (int x = 0; x < Static.mapSize * 2; ++x) {
                Vector2Int pos = new(x, y);
                result += new Vector2Int(x, y) + ":";
                for (int i = 0; i < fastestArriveTimes.Get(pos).Length; ++i) {
                    result += Get(pos, i) + ",";
                }
                result +=  " ";
            }
            result += Environment.NewLine;
        }
        return result;
    }
}

public class DictionaryFastestNodes : FastestNodes {
    private Map<AIMapBlock> aiMap;
    private Dictionary<PosInterval, SearchNode> fastestArriveTimes = new();

    public DictionaryFastestNodes(Map<AIMapBlock> aiMap) {
        this.aiMap = aiMap;
    }

    public override void Set(Vector2Int pos, int intervalIndex, SearchNode node) {
        fastestArriveTimes[new(pos, intervalIndex)] = node;
    }

    public override SearchNode Get(Vector2Int pos, int intervalIndex) {
        SearchNode result;
        if (fastestArriveTimes.TryGetValue(new(pos, intervalIndex), out result)) {
            return result;
        } else {
            return null;
        }
    }

    public override int GetIntervalCount(Vector2Int pos) {
        return aiMap.Get(AI.PosToMapBlock(pos)).IntervalCount();
    }
}


