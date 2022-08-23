using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AI {
    private const float allowedError = 0.06f;
    public const float enterErrorTime = 0.1f;
    private const float preventDis = 0.01f;
    private const float moveLagErrorTime = 0.02f;
    public const float decideTime = 0.1f;

    private readonly int playerId;

    public static Vector2Int MapPosToMapBlock(Vector2 mapPos) {
        int x = Mathf.FloorToInt(mapPos.x), y = Mathf.FloorToInt(mapPos.y);
        return new(x, y);
    }

    public static Vector2Int PosToMapBlock(Vector2Int pos) {
        int x = pos.x >= 0 ? pos.x / 2 : -1;
        int y = pos.y >= 0 ? pos.y / 2 : -1;
        return new Vector2Int(x, y);
    }

    public static Vector2 PosToMapPos(Vector2Int pos) {
        Vector2Int mapBlock = PosToMapBlock(pos);
        float mapPosX = mapBlock.x;
        if (pos.x % 2 == 0) mapPosX += preventDis + Character.colliderHalfSize.x;
        else mapPosX += 1 - preventDis - Character.colliderHalfSize.x;
        float mapPosY = mapBlock.y;
        if (pos.y % 2 == 0) mapPosY += preventDis + Character.colliderHalfSize.y;
        else mapPosY += 1 - preventDis - Character.colliderHalfSize.y;
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
                if (!map[PosToMapBlock(node.pos)].IsSafe(node.time)) {
                    return false;
                }
                found = true;
                return true;
            });
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

        PriorityQueue<SearchNode> pq = new(allowedError);
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

            //int[] randomOrderOfDirections = Random.RandomPermutation(4, 4);
            for (int i = 0; i < 4; ++i) {
                Vector2Int nextPos = cur.pos + Direction.directions[i].Vector2Int;
                Vector2Int nextMapBlock = PosToMapBlock(nextPos);

                if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;

                AIPredictionMapBlock nextAiMapBlock = map[nextMapBlock];

                if (nextAiMapBlock.IsNoneDestroyable()) continue;

                float transitionTime = PosMapManhattanDistance(cur.pos, nextPos) / character.speed;

                // possible enter times, enter each interval as soon as possible (greedy)
                foreach (float startTime in nextAiMapBlock.TimesToEnter(cur.time)) {
                    float endTime = startTime + transitionTime;
                    int intervalIndex = nextAiMapBlock.GetIntervalIndex(endTime);
                    SearchNode fastestNode = fastestNodes.Get(nextPos, intervalIndex);
                    if (fastestNode != null && fastestNode.time <= endTime
                        || nextAiMapBlock.IsDestroyable(startTime)
                        || !ignoreExplode && (nextAiMapBlock.ExplodeOverlap(startTime, endTime + moveLagErrorTime) || curAiMapBlock.ExplodeOverlap(cur.time, endTime + moveLagErrorTime))
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
        DecideInstructions(source, 0, aiMapGenerator, instructions, additionalEvents, true, false);
        Debug.Log("instructions: " + InstructionsToString(instructions));
        //Debug.Log("additional events: " + EventsToString(additionalEvents));

        stopwatch.Stop();
        Debug.Log("decision time taken: " + stopwatch.ElapsedMilliseconds);
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
        SearchNode targetNode = null;
        AIPredictionEvent newAdditionalEvent = null;
        float highestScore = -99;
        SearchTarget target = null;

        bool TestPutBomb(SearchNode node) {
            Vector2Int mapBlock = PosToMapBlock(node.pos);
            if (character.bombNum.ValueAt(node.time) >= character.bombCapacity || map[mapBlock].Bomb(node.time) != null) return false;
            List<AIPredictionEvent> testAdditionalEvents = new(additionalEvents);
            AIPredictionEvent testAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.BombCreate, node.time, new Bomb(mapBlock, character.bombPower, character.id));
            testAdditionalEvents.Add(testAdditionalEvent);
            if (SearchWayOut(node.pos, node.time, aiPredictionGenerator.Generate(playerId, testAdditionalEvents, assumeCharacterPutBomb), ignoreExplode)) {
                targetNode = node;
                newAdditionalEvent = testAdditionalEvent;
                return true;
            } else {
                return false;
            }
        }

        SearchTarget closestCharacter = new("closestCharacter",
            null,
            (time) => {
                return 3 - 2 * time;
            },
            true,
            false
        );

        List<AIPredictionCharacter> otherCharacters = new();
        foreach (int characterId in prediction.characters.Keys) {
            if (characterId != playerId) otherCharacters.Add(prediction.characters[characterId]);
        }
        otherCharacters.Sort((a, b) => { return PosMapManhattanDistance(a.pos, character.pos).CompareTo(PosMapManhattanDistance(b.pos, character.pos)); });

        int movesWithin = (character.bombPower - 1) * 2;
        foreach (AIPredictionCharacter otherCharacter in otherCharacters) {
            if (closestCharacter.Score((PosMapManhattanDistance(otherCharacter.pos, character.pos) - movesWithin / 2) / character.speed) < highestScore) continue;
            bool OnNodePopCharacter(SearchNode node) {
                if (otherCharacter.pos.Equals(node.pos) && SearchWayOut(node.pos, node.time, prediction, ignoreExplode)) {
                    int moveCount = 0;
                    SearchNode cur = node;
                    List<SearchNode> path = new();
                    while (cur != null && moveCount < movesWithin) {
                        ++moveCount;
                        path.Add(cur);
                        cur = cur.prev;
                    }
                    while (path.Count > 0) {
                        SearchNode curNode = path[^1];
                        path.RemoveAt(path.Count - 1);
                        if (closestCharacter.Score(curNode.time) <= highestScore) break;
                        if (character.bombNum.ValueAt(curNode.time) < character.bombCapacity && TestPutBomb(curNode)) {
                            highestScore = closestCharacter.Score(curNode.time);
                            target = closestCharacter;
                            break;
                        }
                    }
                    return true;
                }
                return false;
            }
            float Heauristic(SearchNode node) { return PosMapManhattanDistance(node.pos, otherCharacter.pos) / character.speed; }
            Search(source, initTime, prediction, ignoreExplode, out _, OnNodePop: OnNodePopCharacter, Heuristic: Heauristic);
        }

        SearchTarget closestDestroyableNeighbor = new("closestDestroyableNeighbor",
            (node) => {
                if (character.bombNum.ValueAt(node.time) >= character.bombCapacity) return false;
                Vector2Int curMapBlock = PosToMapBlock(node.pos);
                for (int i = 0; i < 4; ++i) {
                    Vector2Int nextMapBlock = curMapBlock + Direction.directions[i].Vector2Int;
                    if (nextMapBlock.x < 0 || nextMapBlock.x >= Static.mapSize || nextMapBlock.y < 0 || nextMapBlock.y >= Static.mapSize) continue;
                    if (map[nextMapBlock].IsNotExplodedDestroyable(node.time + Bomb.explodeTime + Explode.extendTime) && TestPutBomb(node)) return true;
                }
                return false;
            },
            (time) => {
                return prediction.destroyableNum.ValueAt(time) > 0 ? 1 - time : -100;
            },
            true,
            false
        );
        SearchTarget closestPossibleCollectable = new("closestPossibleCollectable",
            (node) => {
                Vector2Int mapBlock = PosToMapBlock(node.pos);
                AIPredictionMapBlock predictionMapBlock = map[mapBlock];
                if (predictionMapBlock.IsAfterDestroyableDestroyed(node.time) && !predictionMapBlock.IsVisited(node.time) && SearchWayOut(node.pos, node.time, prediction, ignoreExplode)) {
                    targetNode = node;
                    newAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.CharacterVisit, node.time);
                    return true;
                }
                return false;
            },
            (time) => {
                bool willHavePossibleCollectable = !prediction.possibleCollectableNum.ValueBetweenTimeSatisfies((num) => { return num == 0; }, time);
                return willHavePossibleCollectable ? 2f - time : -100;
            },
            false,
            false
        );
        SearchTarget closestCollectable = new("closestCollectable",
            (node) => {
                Vector2Int mapBlock = PosToMapBlock(node.pos);
                AIPredictionMapBlock predictionMapBlock = map[mapBlock];
                if (predictionMapBlock.IsCollectable(node.time) && SearchWayOut(node.pos, node.time, prediction, ignoreExplode)) {
                    targetNode = node;
                    newAdditionalEvent = new(mapBlock, AIPredictionEvent.Type.CharacterVisit, node.time);
                    return true;
                }
                return false;
            },
            (time) => {
                return prediction.collectableNum.ValueAt(time) > 0 ? 3 - time : -100;
            },
            false,
            false
        );
        SearchTarget closestSafe = new("closestSafe",
            (node) => {
                if (map[PosToMapBlock(node.pos)].IsSafe(node.time)) {
                    targetNode = node;
                    newAdditionalEvent = null;
                    return true;
                } else {
                    return false;
                }
            },
            (time) => {
                return -98;
            },
            false,
            true
        );
        SearchTarget[] searchTargets = {closestDestroyableNeighbor, closestCollectable, closestPossibleCollectable, closestSafe };
        int searchingTargets = searchTargets.Length;
        bool OnNodePopOthers(SearchNode node) {
            foreach (SearchTarget searchTarget in searchTargets) {
                if (!searchTarget.searching) continue;
                float score = searchTarget.Score(node.time);
                if (score <= highestScore) {
                    searchTarget.searching = false;
                    --searchingTargets;
                } else if (searchTarget.Found(node)) {
                    target = searchTarget;
                    searchTarget.searching = false;
                    --searchingTargets;
                    highestScore = score;
                }
            }
            return searchingTargets == 0;
        }

        Search(source, initTime, prediction, ignoreExplode, out FastestNodes fastestNodes, OnNodePop: OnNodePopOthers);
        //Debug.Log(prediction.map);
        //Debug.Log(fastestNodes);

        if (targetNode == null) {
            if (assumeCharacterPutBomb) {
                //Debug.Log("disable character bomb");
                DecideInstructions(source, initTime, aiPredictionGenerator, instructions, additionalEvents, false, ignoreExplode);
            } else if (!ignoreExplode) {
                //Debug.Log("ignore explode");
                DecideInstructions(source, initTime, aiPredictionGenerator, instructions, additionalEvents, assumeCharacterPutBomb, true);
            } else {
                //Debug.Log("no way to go");
                instructions.Add(new(source, decideTime, waitTime: decideTime));
            }
        } else {
            //Debug.Log(targetNode.pos + " " + target.tag + " at " + targetNode.time);
            GetInstructions(targetNode, target.putBomb, instructions);
            float lastInstructionTime = instructions.Count > 0 ? instructions[^1].time : initTime;
            if (newAdditionalEvent != null && newAdditionalEvent.time <= lastInstructionTime) additionalEvents.Add(newAdditionalEvent);
            if (instructions.Count == 0 || instructions[^1].time < decideTime) {
                if (depth >= 8) {
                    //Debug.Log("decide instruction too deep");
                    return;
                }
                if (target.waitRemain) {
                    Instruction waitRemain = instructions.Count > 0 ?
                        new(instructions[^1].pos, decideTime, waitTime: decideTime - instructions[^1].time) :
                        new(source, decideTime, waitTime: decideTime);
                    instructions.Add(waitRemain);
                } else {
                    DecideInstructions(targetNode.pos, targetNode.time, aiPredictionGenerator, instructions, additionalEvents, assumeCharacterPutBomb, ignoreExplode, ++depth);
                }
            }
        }
    }
}

public class SearchTarget {
    public bool searching = true;
    public Func<SearchNode, bool> Found;
    public Func<float, float> Score;
    public string tag;
    public bool putBomb;
    public bool waitRemain;

    public SearchTarget(string tag, Func<SearchNode, bool> Found, Func<float, float> Score, bool putBomb, bool waitRemain) {
        this.tag = tag;
        this.Found = Found;
        this.Score = Score;
        this.putBomb = putBomb;
        this.waitRemain = waitRemain;
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
        return aiMap[AI.PosToMapBlock(pos)].IntervalCount();
    }
}


