using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMapGenerator {
    private List<AIMapEvent> events;
    private Map<MapBlock> blocks;
    private List<AIMapCharacter> characters;
    private float shiftTime;

    public AIMapGenerator(float shiftTime, List<AIMapEvent> nextEvents = null) {
        this.shiftTime = shiftTime;
        events = new();
        blocks = new(Static.mapSize);
        characters = new();
        for (int x = 0; x < Static.mapSize; x++) {
            for (int y = 0; y < Static.mapSize; y++) {
                Vector2Int mapBlock = new(x, y);
                GameObject go = Static.map[mapBlock];
                MapBlock block = null;
                if (go != null) {
                    if (go.CompareTag("Destroyable")) block = new(MapBlock.Type.Destroyable);
                    if (go.CompareTag("NoneDestroyable")) block = new(MapBlock.Type.NoneDestroyable);
                    if (go.CompareTag("Collectable")) block = new(MapBlock.Type.Collectable);
                    if (go.CompareTag("Bomb")) {
                        BombController bombController = go.GetComponent<BombController>();
                        Bomb bomb = bombController.Bomb;
                        block = new(MapBlock.Type.Bomb, bomb);
                        events.Add(new(bomb.MapPos, AIMapEvent.Type.BombExplode, bombController.TimeToExplode - shiftTime, bomb));
                    }
                    if (go.CompareTag("Explode")) {
                        ExplodeController explodeController = go.GetComponent<ExplodeController>();
                        Explode explode = explodeController.Explode;
                        block = new(MapBlock.Type.Explode, explode);
                        if (explodeController.TimeToExplode > 0) {
                            events.Add(new(explode.MapPos, AIMapEvent.Type.ExplodeExtend, explodeController.TimeToExplode - shiftTime, explode));
                        }
                        events.Add(new(explode.MapPos, AIMapEvent.Type.ExplodeDestroy, explodeController.TimeToDestroy - shiftTime, explode));
                    }
                }
                blocks[new(x, y)] = block;
            }
        }
        foreach (Player player in Player.livingPlayers) {
            Character character = player.Character;
            characters.Add(new(character.Id, character.Position, character.BombPower, character.BombNum, character.BombCapacity));
        }
        if (nextEvents != null) {
            foreach (AIMapEvent aiEvent in nextEvents) {
                aiEvent.time -= AI.decideTime;
                events.Add(aiEvent);
            }
        }
    }

    public Map<AIMapBlock> Generate(int playerId, List<AIMapEvent> additionalEvents, bool assumeCharacterPutBomb) {
        PriorityQueue<AIMapEvent, float> pq = new();
        Map<AIMapBlock> aiMap = new(Static.mapSize);
        for (int x = 0; x < Static.mapSize; x++) {
            for (int y = 0; y < Static.mapSize; y++) {
                Vector2Int mapBlock = new(x, y);
                AIMapBlock aIMapBlock = new();
                MapBlock block = blocks[mapBlock];
                if (block != null) {
                    if (block.type == MapBlock.Type.Destroyable) aIMapBlock.isDestroyable = true;
                    if (block.type == MapBlock.Type.NoneDestroyable) aIMapBlock.isNoneDestroyable = true;
                    if (block.type == MapBlock.Type.Collectable) aIMapBlock.isCollectable = true;
                    if (block.type == MapBlock.Type.Bomb) aIMapBlock.bomb = (Bomb)block.element;
                    if (block.type == MapBlock.Type.Explode) aIMapBlock.AddExplodeStart(-shiftTime);
                }
                aiMap[mapBlock] = aIMapBlock;
            }
        }
        foreach (AIMapCharacter character in characters) {
            if (character.id == playerId) continue;
            Vector2Int mapBlock = new((int)Mathf.Floor(character.position.x), (int)Mathf.Floor(character.position.y));
            AIMapBlock aIMapBlock = aiMap[mapBlock];
            aIMapBlock.character = character;
            if (assumeCharacterPutBomb) {
                pq.Add(new(mapBlock, AIMapEvent.Type.BombCreate, 0, new Bomb(mapBlock, character.bombPower)), 0);
            }
        }
        foreach (AIMapEvent aiEvent in events) {
            pq.Add(aiEvent, aiEvent.time);
        }
        if (additionalEvents != null) {
            foreach(AIMapEvent aiEvent in additionalEvents) {
                pq.Add(aiEvent, aiEvent.time);
            }
        }
        if (assumeCharacterPutBomb) {

        }
        while (!pq.Empty()) {
            AIMapEvent aiMapEvent = pq.Pop();
            aiMapEvent.RunEvent(aiMap, pq);
        }

        return aiMap;
    }
}

public class AIMapCharacter {
    public int id;
    public Vector2 position;
    public int bombPower;
    public int bombsNum;
    public int bombCapacity;
    public AIMapCharacter(int id, Vector2 position, int bombPower, int bombNum, int bombCapacity) {
        this.id = id;
        this.position = position;
        this.bombPower = bombPower;
        this.bombsNum = bombNum;
        this.bombCapacity = bombCapacity;
    }
}

public class AIMapEvent {
    public enum Type { BombCreate, BombExplode, ExplodeExtend, ExplodeDestroy, CollectableDestroy };
    public float time;
    public Type type;
    public object element;
    public Vector2Int mapBlock;

    public AIMapEvent(Vector2Int mapBlock, Type type, float time = -1, object element = null) {
        this.time = time;
        this.type = type;
        this.element = element;
        this.mapBlock = mapBlock;
    }

    public void RunEvent(Map<AIMapBlock> aiMap, PriorityQueue<AIMapEvent, float> pq) {
        AIMapBlock aIMapBlock = aiMap[mapBlock];
        if (type == Type.ExplodeExtend) {
            Explode explode = (Explode)element;
            explode.CreateNextExplode((nextMapBlock, powerLeft, direction) => {
                AIMapBlock nextAIMapBlock = aiMap[nextMapBlock];
                if (nextAIMapBlock.isNoneDestroyable) return;
                if (nextAIMapBlock.isDestroyable) {
                    nextAIMapBlock.isDestroyable = false;
                    nextAIMapBlock.destroyableEnd = time;
                    return;
                }
                if (nextAIMapBlock.bomb != null) {
                    pq.Remove(new(nextMapBlock, Type.BombExplode));
                    BombExplode(nextAIMapBlock.bomb, time);
                    return;
                }
                Explode explode = new Explode(nextMapBlock, powerLeft, direction);
                nextAIMapBlock.AddExplodeStart(time);
                pq.Add(new(nextMapBlock, Type.ExplodeExtend, time + Explode.explodeInterval, explode), time + Explode.explodeInterval);
                pq.Add(new(nextMapBlock, Type.ExplodeDestroy, time + explode.ExistTime, explode), time + explode.ExistTime);
            });
        } else if (type == Type.BombExplode) {
            BombExplode((Bomb)element, time);
        } else if (type == Type.ExplodeDestroy) {
            aIMapBlock.AddExplodeEnd(time);
        } else if (type == Type.BombCreate){
            Bomb bomb = (Bomb)element;
            aIMapBlock.bomb = bomb;
            pq.Add(new(mapBlock, Type.BombExplode, time + Bomb.explodeTime, bomb), time + Bomb.explodeTime);
        } else if (type == Type.CollectableDestroy) {
            aIMapBlock.isCollectable = false;
            aIMapBlock.collectableEnd = time;
        }

        void BombExplode(Bomb bomb, float time) {
            Explode explode = new Explode(bomb.MapPos, bomb.BombPower, Direction.None);
            AIMapBlock aIMapBlock = aiMap[bomb.MapPos];
            aIMapBlock.AddExplodeStart(time);
            pq.Add(new(bomb.MapPos, Type.ExplodeExtend, time + Explode.explodeInterval, explode), time + Explode.explodeInterval);
            pq.Add(new(bomb.MapPos, Type.ExplodeDestroy, time + explode.ExistTime, explode), time + explode.ExistTime);
            aIMapBlock.bomb = null;
            aIMapBlock.bombEnd = time;
        }
    }

    public override bool Equals(object obj) {
        if (obj is AIMapEvent) {
            AIMapEvent e = (AIMapEvent)obj;
            return mapBlock.Equals(e.mapBlock) && type == e.type;
        } else {
            return false;
        }
    }

    public override int GetHashCode() {
        return mapBlock.GetHashCode() ^ type.GetHashCode();
    }
}

public class MapBlock {
    public enum Type { Destroyable, NoneDestroyable, Bomb, Explode, Collectable};
    public Type type;
    public object element;
    public MapBlock(Type type, object element = null) {
        this.type = type;
        this.element = element;
    }
}

public class AIMapBlock {
    private List<float> exStart = new();
    private int exNum = 0;
    private List<float> exEnd = new();
    public Bomb bomb;
    public float bombEnd = -1;
    public bool isDestroyable = false;
    public float destroyableEnd = -1;
    public bool isNoneDestroyable = false;
    public AIMapCharacter character;
    public bool isCollectable = false;
    public float collectableEnd = -1;

    public int IntervalCount() {
        return exStart.Count + 1;
    }

    public int GetIntervalIndex(float time) {
        int index = 0;
        for (int i = 0; i < exStart.Count; ++i) {
            if (time >= exEnd[i]) ++index;
        }
        return index;
    }

    public bool Empty(float time) {
        return !isNoneDestroyable
            && !IsDestroyable(time)
            && !IsBomb(time)
            && TimesToEnter(time).Count == 1;
    }

    public bool ExplodeOverlap(float start, float end) {
        for (int i = 0; i < exStart.Count; ++i) {
            if (start < exEnd[i] && end > exStart[i]) return true;
        }
        return false;
    }

    public bool IsDestroyable(float time) {
        return isDestroyable || (destroyableEnd > 0 && time < destroyableEnd);
    }

    public bool IsBomb(float time) {
        return bombEnd > 0 && time < bombEnd;
    }

    public bool IsCollectable(float time) {
        return isCollectable || (collectableEnd > 0 && time < collectableEnd);
    }

    public List<float> TimesToEnter(float time) {
        List<float> result = new();
        result.Add(time);
        if (destroyableEnd > 0 && destroyableEnd > time) result.Add(destroyableEnd);
        for (int i = 0; i < exStart.Count; ++i) {
            if (exEnd[i] > time) result.Add(exEnd[i]);
        }
        return result;
    }

    public void AddExplodeStart(float time) {
        if (exNum == 0) {
            exStart.Add(time);
        }
        exNum++;
    }

    public void AddExplodeEnd(float time) {
        exNum--;
        if (exNum == 0) {
            exEnd.Add(time);
        }
    }

    public override string ToString() {
        string print = "";
        for (int i = 0; i < exStart.Count; ++i) {
            print += "[" + exStart[i] + "," + exEnd[i] + "], ";
        }
        return print;
    }
}

public class TimedValue<T> {
    private List<TimeValuePair<T>> timeValuePairs = new();
    
    public void Add(TimeValuePair<T> timeValuePair) {
        timeValuePairs.Add(timeValuePair);
    }

    public T ValueAt(float time) {
        T value = default(T);
        foreach (TimeValuePair<T> timeValuePair in timeValuePairs) {
            if (timeValuePair.time <= time) {
                value = timeValuePair.value;
            } else {
                break;
            }
        }
        return value;
    }
}

public struct TimeValuePair<T> {
    public float time;
    public T value;

    public TimeValuePair(float time, T value) {
        this.time = time;
        this.value = value;
    }
}

//public class TimeExplode : TimedValue<int> {
//    public TimeExplode
//}
