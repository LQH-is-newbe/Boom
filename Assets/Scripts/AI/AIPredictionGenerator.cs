using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPredictionGenerator {
    private readonly List<AIPredictionEvent> events = new();
    private readonly AIPrediction predictionTemplate = new();

    public AIPredictionGenerator(float shiftTime, List<AIPredictionEvent> nextEvents = null) {
        for (int x = 0; x < Static.mapSize; x++) {
            for (int y = 0; y < Static.mapSize; y++) {
                Vector2Int mapBlock = new(x, y);
                GameObject go = Static.map[mapBlock];

                bool isDestroyable = false;
                bool isNoneDestroyable = false;
                bool isCollectable = false;
                Bomb bomb = null;
                int explodes = 0;

                if (go != null) {
                    if (go.CompareTag("Destroyable")) isDestroyable = true;
                    if (go.CompareTag("NoneDestroyable")) isNoneDestroyable = true;
                    if (go.CompareTag("Collectable")) isCollectable = true;
                    if (go.CompareTag("Bomb")) {
                        BombController bombController = go.GetComponent<BombController>();
                        bomb = bombController.Bomb;
                        events.Add(new(bomb.MapBlock, AIPredictionEvent.Type.BombExplode, bombController.TimeToExplode - shiftTime, bomb));
                    }
                    if (go.CompareTag("Explode")) {
                        ExplodeController explodeController = go.GetComponent<ExplodeController>();
                        Explode explode = explodeController.Explode;
                        explodes = 1;
                        if (explodeController.TimeToExplode > 0) {
                            events.Add(new(explode.MapBlock, AIPredictionEvent.Type.ExplodeExtend, explodeController.TimeToExplode - shiftTime, explode));
                        }
                        events.Add(new(explode.MapBlock, AIPredictionEvent.Type.ExplodeDestroy, explodeController.TimeToDestroy - shiftTime, explode));
                    }
                }
                predictionTemplate.map[mapBlock] = new(explodes, bomb, isDestroyable, isCollectable, isNoneDestroyable);
            }
        }
        foreach (Player player in Player.livingPlayers) {
            AIPredictionCharacter character = new(player.Character);
            predictionTemplate.characters[character.id] = character;
        }
        if (nextEvents != null) {
            foreach (AIPredictionEvent predictionEvent in nextEvents) {
                predictionEvent.time -= shiftTime;
                events.Add(predictionEvent);
            }
        }
    }

    public AIPrediction Generate(int playerId, List<AIPredictionEvent> additionalEvents, bool assumeCharacterPutBomb) {
        PriorityQueue<AIPredictionEvent, float> pq = new();
        AIPrediction prediction = predictionTemplate.Copy();
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

    public AIPrediction Copy() {
        AIPrediction copy = new();
        for (int x = 0; x < Static.mapSize; ++x) {
            for (int y = 0; y < Static.mapSize; ++y) {
                Vector2Int mapBlock = new(x, y);
                copy.map[mapBlock] = map[mapBlock].Copy();
            }
        }
        foreach (int id in characters.Keys) {
            copy.characters[id] = characters[id].Copy();
        }
        return copy;
    }
}

public class AIPredictionCharacter {
    public int id;
    public Vector2Int pos;
    public int bombPower;
    public TimedValue<int> bombNum;
    public int bombCapacity;
    public float speed;

    public AIPredictionCharacter(Character character) {
        id = character.Id;
        pos = new((int)Mathf.Floor(character.Position.x / 0.5f), (int)Mathf.Floor(character.Position.y / 0.5f));
        bombPower = character.BombPower;
        bombNum = new(character.BombNum);
        bombCapacity = character.BombCapacity;
        speed = character.Speed;
    }

    public AIPredictionCharacter Copy() {
        return (AIPredictionCharacter)MemberwiseClone();
    }
}

public class AIPredictionEvent {
    public enum Type { BombCreate, BombExplode, ExplodeExtend, ExplodeDestroy, CollectableDestroy };
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

    public void RunEvent(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> pq) {
        Map<AIPredictionMapBlock> map = prediction.map;
        AIPredictionMapBlock predictionMapBlock = map[mapBlock];
        if (type == Type.ExplodeExtend) {
            Explode explode = (Explode)element;
            explode.CreateNextExplode((nextMapBlock, powerLeft, direction) => {
                AIPredictionMapBlock nextPredictionMapBlock = map[nextMapBlock];
                if (nextPredictionMapBlock.IsNoneDestroyable()) return;
                if (nextPredictionMapBlock.IsDestroyable(time)) {
                    nextPredictionMapBlock.DestroyDestroyable(time);
                    return;
                }
                Bomb bomb = nextPredictionMapBlock.Bomb(time);
                if (bomb != null) {
                    pq.Remove(new(nextMapBlock, Type.BombExplode));
                    BombExplode(bomb);
                    return;
                }
                if (nextPredictionMapBlock.IsCollectable(time)) {
                    nextPredictionMapBlock.DestroyDestroyable(time);
                }
                Explode explode = new Explode(nextMapBlock, powerLeft, direction);
                nextPredictionMapBlock.AddExplodeStart(time);
                pq.Add(new(nextMapBlock, Type.ExplodeExtend, time + Explode.explodeInterval, explode), time + Explode.explodeInterval);
                pq.Add(new(nextMapBlock, Type.ExplodeDestroy, time + explode.ExistTime, explode), time + explode.ExistTime);
            });
        } else if (type == Type.BombExplode) {
            BombExplode((Bomb)element);
        } else if (type == Type.ExplodeDestroy) {
            predictionMapBlock.AddExplodeEnd(time);
        } else if (type == Type.BombCreate){
            Bomb bomb = (Bomb)element;
            predictionMapBlock.ChangeBomb(bomb, time);
            prediction.characters[bomb.CreaterId].bombNum.ChangeOnLastValue(time, (bombNum) => { return bombNum + 1; });
            pq.Add(new(mapBlock, Type.BombExplode, time + Bomb.explodeTime, bomb), time + Bomb.explodeTime);
        } else if (type == Type.CollectableDestroy) {
            predictionMapBlock.DestroyCollectable(time);
        }

        void BombExplode(Bomb bomb) {
            Vector2Int mapBlock = bomb.MapBlock;
            Explode explode = new Explode(mapBlock, bomb.BombPower, Direction.None);
            AIPredictionMapBlock predictionMapBlock = map[mapBlock];
            predictionMapBlock.AddExplodeStart(time);
            prediction.characters[bomb.CreaterId].bombNum.ChangeOnLastValue(time, (bombNum) => { return bombNum - 1; });
            pq.Add(new(mapBlock, Type.ExplodeExtend, time + Explode.explodeInterval, explode), time + Explode.explodeInterval);
            pq.Add(new(mapBlock, Type.ExplodeDestroy, time + explode.ExistTime, explode), time + explode.ExistTime);
            predictionMapBlock.ChangeBomb(null, time);
        }
    }

    public override bool Equals(object obj) {
        if (obj is AIPredictionEvent) {
            AIPredictionEvent e = (AIPredictionEvent)obj;
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
    private TimedValue<bool> isCollectable;
    private readonly bool isNoneDestroyable;
    private List<float> intervalDivides;

    public AIPredictionMapBlock(int explodes, Bomb bomb, bool isDestroyable, bool isCollectable, bool isNoneDestroyable) {
        this.explodes = new(explodes);
        this.bomb = new(bomb);
        this.isDestroyable = new(isDestroyable);
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
}

public struct TimedValue<T> {
    private List<TimeValuePair<T>> timeValuePairs;
    private T initialValue;

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
}

public struct TimeValuePair<T> {
    public float time;
    public T value;

    public TimeValuePair(float time, T value) {
        this.time = time;
        this.value = value;
    }
}
