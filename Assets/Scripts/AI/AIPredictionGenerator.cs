using System;
using System.Collections.Generic;
using UnityEngine;

public class AIPredictionGenerator {
    private readonly List<AIPredictionEvent> events = new();
    private readonly Map<MapBlock> mapSnapshot;
    private readonly Dictionary<int, Character> charactersSnapshot;
    private readonly int destroyableNumSnapshot;
    private readonly int collectableNumSnapshot;

    public AIPredictionGenerator(float shiftTime, float envShiftTime, List<AIPredictionEvent> nextEvents = null) {
        mapSnapshot = new(Static.mapSize);
        for (int x = 0; x < Static.mapSize; ++x) {
            for (int y = 0; y < Static.mapSize; ++y) {
                Vector2Int mapBlock = new(x, y);
                MapBlock blockSnapshot = new();
                MapBlock block = Static.mapBlocks[mapBlock];
                mapSnapshot[mapBlock] = blockSnapshot;
                blockSnapshot.element = block.element?.Copy();
                if (block.element is Bomb bomb) {
                    Bomb bombSnapshot = (Bomb)blockSnapshot.element;
                    BombController bombController = (BombController)Static.controllers[bomb];
                    events.Add(new(bombSnapshot.MapBlock, AIPredictionEvent.Type.BombTrigger, bombController.TimeToExplode - shiftTime + envShiftTime, bombSnapshot));
                }
                if (block.element is Destroyable destroyable && destroyable.exploding) {
                    Destroyable destroyableSnapshot = (Destroyable)blockSnapshot.element;
                    DestroyableController destroyableController = (DestroyableController)Static.controllers[destroyable];
                    events.Add(new(destroyableSnapshot.MapBlock, AIPredictionEvent.Type.DestroyableDestroy, destroyableController.TimeToDestroy - shiftTime + envShiftTime, destroyableSnapshot));
                }
                for (int i = 0; i < block.explodes.Count; ++i) {
                    Explode explode = block.explodes[i];
                    blockSnapshot.explodes.Add(explode.Copy());
                    Explode explodeSnapshot = blockSnapshot.explodes[i];
                    ExplodeController explodeController = (ExplodeController)Static.controllers[explode];
                    if (explodeController.TimeToExtend > 0) {
                        events.Add(new(explodeSnapshot.MapBlock, AIPredictionEvent.Type.ExplodeExtend, explodeController.TimeToExtend - shiftTime + envShiftTime, explodeSnapshot));
                    }
                    events.Add(new(explodeSnapshot.MapBlock, AIPredictionEvent.Type.ExplodeDestroy, explodeController.TimeToDestroy + AI.enterErrorTime - shiftTime + envShiftTime, explodeSnapshot));
                }
            }
        }
        destroyableNumSnapshot = Static.notExplodedDestroyableNum;
        collectableNumSnapshot = Static.collectables.Count;

        charactersSnapshot = new();
        foreach (int id in Character.characters.Keys) {
            charactersSnapshot[id] = Character.characters[id].Copy();
        }

        // TODO: copy events?
        if (nextEvents != null) {
            foreach (AIPredictionEvent predictionEvent in nextEvents) {
                predictionEvent.time -= shiftTime;
                events.Add(predictionEvent);
            }
        }
    }

    public AIPrediction Generate(int playerId, List<AIPredictionEvent> additionalEvents, bool assumeCharacterPutBomb) {
        PriorityQueue<AIPredictionEvent> pq = new();
        AIPrediction prediction = new();
        prediction.destroyableNum = new(destroyableNumSnapshot);
        prediction.collectableNum = new(collectableNumSnapshot);
        for (int x = 0; x < Static.mapSize; x++) {
            for (int y = 0; y < Static.mapSize; y++) {
                Vector2Int mapBlock = new(x, y);
                MapBlock block = mapSnapshot[mapBlock];

                bool isDestroyable = false;
                bool isNotExplodedDestroyable = false;
                bool isNoneDestroyable = false;
                bool isCollectable = false;
                Bomb bomb = null;
                int explodes = 0;

                if (block.element is Destroyable destroyable) {
                    isDestroyable = true;
                    if (!destroyable.exploding) isNotExplodedDestroyable = true;
                } 
                else if (block.element is NoneDestroyable) isNoneDestroyable = true;
                else if (block.element is Collectable) isCollectable = true;
                else if (block.element is Bomb bomb1) bomb = bomb1;
                foreach (Explode explode in block.explodes) {
                    ++explodes;
                }
                prediction.map[mapBlock] = new(explodes, bomb, isDestroyable, isNotExplodedDestroyable, isCollectable, isNoneDestroyable);
            } 
        }

        foreach (int id in charactersSnapshot.Keys) {
            prediction.characters[id] = new(charactersSnapshot[id]);
        }

        if (assumeCharacterPutBomb) {
            foreach (int characterId in prediction.characters.Keys) {
                if (characterId == playerId) continue;
                AIPredictionCharacter character = prediction.characters[characterId];
                Vector2Int mapBlock = AI.PosToMapBlock(character.pos);
                pq.Add(new(mapBlock, AIPredictionEvent.Type.BombCreate, 0, new Bomb(mapBlock, character.bombPower, character.id)), 0);
            }
        }
        foreach (AIPredictionEvent aiEvent in events) {
            pq.Add(aiEvent, aiEvent.time);
        }
        if (additionalEvents != null) {
            foreach(AIPredictionEvent aiEvent in additionalEvents) {
                pq.Add(aiEvent, aiEvent.time);
            }
        }
        while (!pq.Empty()) {
            AIPredictionEvent aiMapEvent = pq.Pop();
            aiMapEvent.RunEvent(prediction, pq);
        }
        return prediction;
    }
}

public class AIPrediction {
    public Map<AIPredictionMapBlock> map = new(Static.mapSize);
    public Dictionary<int, AIPredictionCharacter> characters = new();
    public TimedValue<int> destroyableNum;
    public TimedValue<int> collectableNum;
    public TimedValue<int> possibleCollectableNum;
}

public class AIPredictionCharacter {
    public int id;
    public Vector2Int pos;
    public int bombPower;
    public TimedValue<int> bombNum;
    public int bombCapacity;
    public float speed;
    public Vector2 position;

    public AIPredictionCharacter(Character character) {
        id = character.Id;
        pos = new((int)Mathf.Floor(character.Position.x / 0.5f), (int)Mathf.Floor(character.Position.y / 0.5f));
        bombPower = character.BombPower;
        bombNum = new(character.BombNum);
        bombCapacity = character.BombCapacity;
        speed = character.Speed;
        position = character.Position;
    }

    public AIPredictionCharacter Copy() {
        return (AIPredictionCharacter)MemberwiseClone();
    }
}

public class AIPredictionEvent {
    public enum Type { BombCreate, BombTrigger, ExplodeExtend, ExplodeDestroy, DestroyableDestroy, CharacterVisit };
    public float time;
    public Type type;
    public object element;
    public Vector2Int mapBlock;

    public AIPredictionEvent(Vector2Int mapBlock, Type type, float time = -1, object element = null) {
        this.time = time;
        this.type = type;
        this.element = element;
        this.mapBlock = mapBlock;
    }

    public void RunEvent(AIPrediction prediction, PriorityQueue<AIPredictionEvent> pq) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[mapBlock];
        if (type == Type.ExplodeExtend) {
            ((Explode)element).ExtendPrediction(prediction, pq, time);
        } else if (type == Type.BombTrigger) {
            ((Bomb)element).TriggerPrediction(prediction, pq, time);
        } else if (type == Type.ExplodeDestroy) {
            predictionMapBlock.AddExplodeEnd(time);
        } else if (type == Type.BombCreate){
            ((Bomb)element).CreatePrediction(prediction, pq, time);
        } else if (type == Type.DestroyableDestroy) {
            predictionMapBlock.DestroyDestroyable(time);
            prediction.possibleCollectableNum.ChangeOnLastValue(time, (num) => { return num + 1; });
        } else if (type == Type.CharacterVisit) {
            predictionMapBlock.Visit(time);
            if (predictionMapBlock.IsAfterDestroyableDestroyed(time) && !predictionMapBlock.IsVisited(time)) {
                prediction.possibleCollectableNum.ChangeOnLastValue(time, (num) => { return num - 1; });
            }
            if (predictionMapBlock.IsCollectable(time)) {
                predictionMapBlock.DestroyCollectable(time);
                prediction.collectableNum.ChangeOnLastValue(time, (num) => { return num - 1; });
            }
        }
    }

    public override bool Equals(object obj) {
        if (obj is AIPredictionEvent e) {
            return mapBlock.Equals(e.mapBlock) && type == e.type;
        } else {
            return false;
        }
    }

    public override int GetHashCode() {
        return mapBlock.GetHashCode() ^ type.GetHashCode();
    }

    public override string ToString() {
        return mapBlock.ToString() + ":" + type.ToString();
    }
}

public class AIPredictionMapBlock {
    private TimedValue<int> explodes;
    private TimedValue<Bomb> bomb;
    private TimedValue<bool> isDestroyable;
    private TimedValue<bool> isNotExplodedDestroyable;
    private TimedValue<bool> isCollectable;
    private TimedValue<bool> visited = new(false);
    private readonly bool isNoneDestroyable;
    private List<float> intervalDivides;

    public AIPredictionMapBlock(int explodes, Bomb bomb, bool isDestroyable, bool isNotExplodedDestroyable, bool isCollectable, bool isNoneDestroyable) {
        this.explodes = new(explodes);
        this.bomb = new(bomb);
        this.isDestroyable = new(isDestroyable);
        this.isNotExplodedDestroyable = new(isNotExplodedDestroyable);
        this.isCollectable = new(isCollectable);
        this.isNoneDestroyable = isNoneDestroyable;
    }

    public AIPredictionMapBlock Copy() {
        return (AIPredictionMapBlock)MemberwiseClone();
    }

    private void AddToIntervalDivides(float time) {
        if (intervalDivides == null) intervalDivides = new();
        intervalDivides.Add(time);
    }

    public void AddExplodeStart(float time) {
        explodes.ChangeOnLastValue(time, (explodes) => { return explodes + 1; });
    }

    public void AddExplodeEnd(float time) {
        explodes.ChangeOnLastValue(time, (explodes) => { return explodes - 1; });
        if (explodes.LastValue() == 0) AddToIntervalDivides(time);
    }

    public void ChangeBomb(Bomb value, float time) {
        bomb.Add(time, value);
    }

    public void DestroyDestroyable(float time) {
        isDestroyable.Add(time, false);
        AddToIntervalDivides(time);
    }

    public void ExplodeDestroyable(float time) {
        isNotExplodedDestroyable.Add(time, false);
    }

    public void Visit(float time) {
        visited.Add(time, true);
    }

    public void DestroyCollectable(float time) {
        isCollectable.Add(time, false);
    }

    public int IntervalCount() {
        if (intervalDivides == null) return 1;
        return intervalDivides.Count + 1;
    }

    public int GetIntervalIndex(float time) {
        if (intervalDivides == null) return 0;
        int index = 0;
        for (int i = 0; i < intervalDivides.Count; ++i) {
            if (time >= intervalDivides[i]) ++index;
        }
        return index;
    }

    public bool IsSafe(float time) {
        return !isNoneDestroyable && TimesToEnter(time).Count == 1;
    }

    public bool IsDestroyable(float time) {
        return isDestroyable.ValueAt(time);
    }

    public bool IsNotExplodedDestroyable(float time) {
        return isNotExplodedDestroyable.ValueAt(time);
    }

    public bool IsAfterDestroyableDestroyed(float time) {
        return isDestroyable.initialValue && !isDestroyable.ValueAt(time);
    }

    public bool IsVisited(float time) {
        return visited.ValueAt(time);
    }

    public Bomb Bomb(float time) {
        return bomb.ValueAt(time);
    }

    public bool IsCollectable(float time) {
        return isCollectable.ValueAt(time);
    }

    public bool IsNoneDestroyable() {
        return isNoneDestroyable;
    }

    public bool ExplodeOverlap(float start, float end) {
        return !explodes.ValueBetweenTimeSatisfies((explodes) => { return explodes == 0; }, start, end);
    }

    public List<float> TimesToEnter(float time) {
        List<float> result = new();
        result.Add(time);
        if (intervalDivides == null) return result;
        for (int i = 0; i < intervalDivides.Count; ++i) {
            if (intervalDivides[i] > time) result.Add(intervalDivides[i]);
        }
        return result;
    }

    public override string ToString() {
        return explodes.ToString();
    }
}

public struct TimedValue<T> {
    private List<TimeValuePair<T>> timeValuePairs;
    public readonly T initialValue;

    public TimedValue(T initialValue) {
        timeValuePairs = null;
        this.initialValue = initialValue;
    }
    
    public void Add(float time, T value) {
        if (timeValuePairs == null) timeValuePairs = new();
        timeValuePairs.Add(new(time, value));
    }

    public T ValueAt(float time) {
        if (timeValuePairs == null) return initialValue;
        int i = timeValuePairs.Count - 1;
        while (i >= 0 && timeValuePairs[i].time > time) --i;
        if (i >= 0) return timeValuePairs[i].value;
        else return initialValue;
    }

    public bool ValueBetweenTimeSatisfies(Func<T, bool> Criterion, float start, float end = 100) {
        if (timeValuePairs == null) return Criterion(initialValue);
        int i = timeValuePairs.Count - 1;
        while (i >= 0 && timeValuePairs[i].time >= end) --i;
        while (i >= 0 && timeValuePairs[i].time > start) {
            if (!Criterion(timeValuePairs[i].value)) return false;
            --i;
        }
        return Criterion(i >= 0 ? timeValuePairs[i].value : initialValue);
    }

    public T LastValue() {
        if (timeValuePairs == null) return initialValue;
        if (timeValuePairs.Count > 0) return timeValuePairs[^1].value;
        else return initialValue;
    }

    public void ChangeOnLastValue(float time, Func<T, T> NewValue) {
        Add(time, NewValue(LastValue()));
    }

    public override string ToString() {
        string result = "(init:" + initialValue + ") ";
        if (timeValuePairs == null) return result;
        foreach (TimeValuePair<T> timeValuePair in timeValuePairs) {
            result += timeValuePair + " ";
        }
        return result;
    }
}

public struct TimeValuePair<T> {
    public float time;
    public T value;

    public TimeValuePair(float time, T value) {
        this.time = time;
        this.value = value;
    }

    public override string ToString() {
        return "(" + time + "," + value + ")";
    }
}
