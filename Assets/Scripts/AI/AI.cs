using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AI {
    private const float preventDis = 0.03f;
    private const float preventTime = 0.05f;
    public const float decideTime = 0.1f;

    private int playerId;

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

    public AI(int playerId) {
        this.playerId = playerId;
    }

    private string InstructionsToString(List<Instruction> instructions) {
        string result = "";
        for (int i = 0; i < instructions.Count; ++i) {
            result += instructions[i] + " ";
        }
        return result;
    }

    private string EventsToString(List<AIPredictionEvent> events) {
        string result = "";
        for (int i = 0; i < events.Count; ++i) {
            result += events[i] + " ";
        }
        return result;
    }

    public List<Instruction> DummyWaitInstruction(Vector2Int pos) {
        List<Instruction> instructions = new();
        instructions.Add(new(pos, decideTime, waitTime: decideTime));
        return instructions;
    }

    private void GetInstructions(SearchNode node, bool putBomb, List<Instruction> currentInstructions) {
        List<Instruction> instructions = new();
        SearchNode cur = node;
        if (putBomb) {
            instructions.Add(new(cur.pos, cur.time, putBomb: true));
        }
        while (cur.prev != null) {
            instructions.Add(new(cur.pos, cur.time));
            if (cur.waitTime > 0) {
                instructions.Add(new(cur.prev.pos, cur.prev.time + cur.waitTime, waitTime: cur.waitTime));
            }
            cur = cur.prev;
        }
        int i = instructions.Count - 1;
        float time = 0;
        while (i >= 0 && instructions[i].time < decideTime) {
            time = instructions[i].time;
            currentInstructions.Add(instructions[i--]);
        }
        if (i >= 0) {
            if (instructions[i].waitTime > 0) {
                Instruction waitRemain = new(instructions[i].pos, decideTime, waitTime: decideTime - time);
                currentInstructions.Add(waitRemain);
            } else {
                currentInstructions.Add(instructions[i]);
                if (i - 1 >= 0 && instructions[i - 1].putBomb) currentInstructions.Add(instructions[i - 1]);
            }
        }
        //} else {
        //    Instruction waitRemain = currentInstructions.Count > 0 ? 
        //        new(currentInstructions[currentInstructions.Count - 1].pos, decideTime, waitTime: decideTime - currentInstructions[currentInstructions.Count - 1].time) :
        //        new(node.pos, decideTime, waitTime: decideTime);
        //    currentInstructions.Add(waitRemain);
        //}
        //Debug.Log(InstructionsToString(result));
    }

    private bool SearchWayOut(Vector2Int source, float initTime, AIPrediction prediction, bool ignoreExplode) {
        Map<AIPredictionMapBlock> map = prediction.map;
        if (map[PosToMapBlock(source)].IsSafe(initTime)) return true;
        bool found = false;
        Search(
            source,
            initTime,
            prediction,
            ignoreExplode,
            out _,
            isQuickSearch: true,
            OnNodePop: (node) => { 
                if (map[PosToMapBlock(node.pos)].IsSafe(node.time)) {
                    found = true;
                    return true;
                }
                return false;
            });
        //if (!found) {
        //    Debug.Log("no way out");
        //    Time.timeScale = 0;
        //}
        return found;
    }

    private void Search(
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
        AIPredictionCharacter character = prediction.characters[playerId];
        int initIntervalIndex = map[PosToMapBlock(source)].GetIntervalIndex(initTime);

        PriorityQueue<SearchNode, float> pq = new();
        SearchNode init = new(source, initIntervalIndex, null, initTime, 0);
        pq.Add(init, Heuristic(init));

        fastestNodes = isQuickSearch ? new DictionaryFastestNodes(map) : new MapArrayFastestNodes(map);
        fastestNodes.Set(source, initIntervalIndex, init);

        int nodes = 0;
        while (!pq.Empty()) {
            ++nodes;
            SearchNode cur = pq.Pop();

            if (OnNodePop(cur)) goto End;

            Vector2Int curMapBlock = PosToMapBlock(cur.pos);
            AIPredictionMapBlock curAiMapBlock = map[curMapBlock];

            for (int i = 0; i < 4; ++i) {
                Vector2Int nextPos = cur.pos + Vector2Int.directions[i];
                Vector2Int nextMapBlock = PosToMapBlock(nextPos);

                if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;

                AIPredictionMapBlock nextAiMapBlock = map[nextMapBlock];

                if (nextAiMapBlock.IsNoneDestroyable()) continue;

                float transitionTime = PosMapManhattanDistance(cur.pos, nextPos) / character.speed;

                // possible enter times, enter each interval as soon as possible (greedy)
                List<float> timesToEnter = nextAiMapBlock.TimesToEnter(cur.time);
                for (int j = 0; j < timesToEnter.Count; ++j) {
                    float startTime = timesToEnter[j] + (j == 0 ? 0: preventTime);
                    float endTime = startTime + transitionTime;
                    int intervalIndex = nextAiMapBlock.GetIntervalIndex(endTime);
                    SearchNode fastestNode = fastestNodes.Get(nextPos, intervalIndex);
                    if (fastestNode != null && fastestNode.time <= endTime
                        || nextAiMapBlock.IsDestroyable(startTime)
                        || !ignoreExplode && (nextAiMapBlock.ExplodeOverlap(startTime, endTime) || curAiMapBlock.ExplodeOverlap(cur.time, endTime))
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

    public Task<Tuple<int, List<Instruction>, List<AIPredictionEvent>>> Decide(int decisionId, Vector2Int source, AIPredictionGenerator aiMapGenerator) {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        List<Instruction> instructions = new();
        List<AIPredictionEvent> additionalEvents = new();
        DecideInstructions(source, 0, aiMapGenerator, instructions, additionalEvents, false, false);
        Debug.Log("instructions: " + InstructionsToString(instructions));
        Debug.Log("additional events: " + EventsToString(additionalEvents));

        stopwatch.Stop();
        return Task.FromResult<Tuple<int, List<Instruction>, List<AIPredictionEvent>>>(new(decisionId, instructions, additionalEvents));
    }

    private void DecideInstructions(
        Vector2Int source, 
        float initTime, 
        AIPredictionGenerator aiPredictionGenerator, 
        List<Instruction> instructions,
        List<AIPredictionEvent> additionalEvents,
        bool assumeCharacterPutBomb,
        bool ignoreExplode,
        int depth = 0) {

        AIPrediction prediction = aiPredictionGenerator.Generate(playerId, additionalEvents, assumeCharacterPutBomb);
        Map<AIPredictionMapBlock> map = prediction.map;
        AIPredictionCharacter character = prediction.characters[playerId];
        float destroyableExistRatioThreshold = 0.5f;
        float destroyableExistRatio = Static.destroyables.Count / (float)Static.totalDestroyableNum;
        bool putBomb = false;
        SearchNode targetNode = null;
        AIPredictionEvent newAdditionalEvent = null;
        Func<SearchNode, bool> TestPutBomb = (node) => {
            Vector2Int mapBlock = PosToMapBlock(node.pos);
            if (character.bombNum.ValueAt(node.time) >= character.bombCapacity || map[mapBlock].Bomb(node.time) != null) return false;
            List<AIPredictionEvent> testAdditionalEvents = new(additionalEvents);
            AIPredictionEvent testAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.BombCreate, node.time, new Bomb(mapBlock, character.bombPower, character.id));
            testAdditionalEvents.Add(testAdditionalEvent);
            if (SearchWayOut(node.pos, node.time, aiPredictionGenerator.Generate(playerId, testAdditionalEvents, assumeCharacterPutBomb), ignoreExplode)) {
                targetNode = node;
                putBomb = true;
                newAdditionalEvent = testAdditionalEvent;
                return true;
            } else {
                return false;
            }
        };
        SearchTarget closestDestroyableNeighbor = new("closestDestroyableNeighbor",
            (node) => {
                Vector2Int curMapBlock = PosToMapBlock(node.pos);
                for (int i = 0; i < 4; ++i) {
                    Vector2Int nextMapBlock = PosToMapBlock(node.pos + Vector2Int.directions[i]);
                    if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;
                    if (map[nextMapBlock].IsDestroyable(node.time + Bomb.explodeTime + Explode.explodeInterval)) {
                        if (TestPutBomb(node)) return true;
                    }
                }
                return false;
            },
            (time) => {
                return Static.destroyables.Count > 0 && character.bombNum.ValueAt(time) < character.bombCapacity ?
                    destroyableExistRatio / destroyableExistRatioThreshold - time : -100;
            }
        );
        SearchTarget closestCollectable = new("closestCollectable",
            (node) => {
                Vector2Int mapBlock = PosToMapBlock(node.pos);
                AIPredictionMapBlock aIMapBlock = map[mapBlock];
                if (map[PosToMapBlock(node.pos)].IsCollectable(node.time) && SearchWayOut(node.pos, node.time, prediction, ignoreExplode)) {
                    targetNode = node;
                    putBomb = false;
                    newAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.CollectableDestroy, node.time, new Collectable(mapBlock, null));
                    return true;
                }
                return false;
            },
            (time) => {
                return Static.collectables.Count > 0 ? 3 - time : -100;
            }
        );
        SearchTarget closestCharacter = new("closestCharacter",
            (node) => {
                Vector2Int mapBlock = PosToMapBlock(node.pos);
                AIPredictionMapBlock aIMapBlock = map[mapBlock];
                foreach (int characterId in prediction.characters.Keys) {
                    if (characterId == playerId) continue;
                    AIPredictionCharacter otherCharacter = prediction.characters[characterId];
                    if (PosToMapBlock(otherCharacter.pos).Equals(mapBlock)) {
                        int moveCount = 0;
                        SearchNode cur = node;
                        List<SearchNode> path = new();
                        while (cur != null && moveCount < 8) {
                            ++moveCount;
                            path.Add(cur);
                            cur = cur.prev;
                        }
                        while (path.Count > 0) {
                            SearchNode curNode = path[^1];
                            path.RemoveAt(path.Count - 1);
                            if (TestPutBomb(curNode)) {
                                return true;
                            } else {
                                //Debug.Log("put bomb character false");
                                //Time.timeScale = 0;
                            }
                        }
                    }
                }
                return false;
            }, 
            (time) => {
                //Debug.Log(character.bombNum.ValueAt(time));
                return character.bombNum.ValueAt(time) < character.bombCapacity ? -50 + destroyableExistRatioThreshold / destroyableExistRatio - time : -100;
            }
        );
        SearchTarget closestSafe = new("closestSafe",
            (node) => {
                return map[PosToMapBlock(node.pos)].IsSafe(node.time);
            },
            (time) => {
                return -98;
            }
        );
        SearchTarget[] searchTargets = {closestDestroyableNeighbor, closestCollectable, closestCharacter};
        int searchingTargets = searchTargets.Length;
        float highestScore = -99;
        Func<SearchNode, bool> OnNodePop = (node) => {
            foreach (SearchTarget searchTarget in searchTargets) {
                if (!searchTarget.searching) continue;
                float score = searchTarget.Score(node.time);
                if (score <= highestScore) {
                    searchTarget.searching = false;
                    --searchingTargets;
                } else if (searchTarget.Found(node)) {
                    searchTarget.searching = false;
                    --searchingTargets;
                    highestScore = score;
                }
            }
            return searchingTargets == 0;
        };

        FastestNodes fastestNodes;
        Search(source, initTime, prediction, ignoreExplode, out fastestNodes, OnNodePop: OnNodePop);

        if (targetNode == null) {
            instructions.Add(new(source, decideTime, waitTime: decideTime));
            if (assumeCharacterPutBomb) {
                DecideInstructions(source, initTime, aiPredictionGenerator, instructions, additionalEvents, false, ignoreExplode);
            } else if (!ignoreExplode) {
                DecideInstructions(source, initTime, aiPredictionGenerator, instructions, additionalEvents, assumeCharacterPutBomb, true);
            } else {
                instructions.Add(new(source, decideTime, waitTime: decideTime));
            }
        } else {
            GetInstructions(targetNode, putBomb, instructions);
            float lastInstructionTime = instructions.Count > 0 ? instructions[^1].time : initTime;
            if (newAdditionalEvent != null && newAdditionalEvent.time <= lastInstructionTime) additionalEvents.Add(newAdditionalEvent);
            //Debug.Log(InstructionsToString(instructions));
            //Debug.Log(EventsToString(additionalEvents));
            if (instructions.Count == 0 || instructions[^1].time < decideTime) {
                if (depth >= 8) {
                    Time.timeScale = 0;
                    Debug.Log("decide instruction too deep");
                    return;
                }
                DecideInstructions(targetNode.pos, targetNode.time, aiPredictionGenerator, instructions, additionalEvents, assumeCharacterPutBomb, ignoreExplode, ++depth);
            }
        }
    }
}

public class SearchTarget {
    public bool searching = true;
    public Func<SearchNode, bool> Found;
    public Func<float, float> Score;
    //public Action OnBeTarget;
    public string tag;

    public SearchTarget(string tag, Func<SearchNode, bool> Found, Func<float, float> Score) {
        this.tag = tag;
        this.Found = Found;
        this.Score = Score;
        //this.OnBeTarget = OnBeTarget;
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

    public MapArrayFastestNodes(Map<AIPredictionMapBlock> aiMap) {
        fastestArriveTimes = new(Static.mapSize * 2);
        for (int x = 0; x < Static.mapSize * 2; x++) {
            for (int y = 0; y < Static.mapSize * 2; y++) {
                Vector2Int curPos = new Vector2Int(x, y);
                fastestArriveTimes[curPos] = new SearchNode[aiMap[AI.PosToMapBlock(curPos)].IntervalCount()];
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
                result +=  " ";
            }
            result += Environment.NewLine;
        }
        return result;
    }
}

public class DictionaryFastestNodes : FastestNodes {
    private Map<AIPredictionMapBlock> aiMap;
    private Dictionary<PosInterval, SearchNode> fastestArriveTimes = new();

    public DictionaryFastestNodes(Map<AIPredictionMapBlock> aiMap) {
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
        return aiMap[AI.PosToMapBlock(pos)].IntervalCount();
    }
}


