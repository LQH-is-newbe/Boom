using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMap {
    public static Map<AIMapBlock> Generate(int playerId) {
        PriorityQueue<Explodable, float> pq = new();
        Map<AIMapBlock> aiMap = new(Static.mapSize);
        for (int x = 0; x < Static.mapSize; x++) {
            for (int y = 0; y < Static.mapSize; y++) {
                Vector2Int mapBlock = new(x, y);
                GameObject go = Static.map.Get(mapBlock);
                AIMapBlock aIMapBlock = new();
                if (go != null) {
                    if (go.CompareTag("Destroyable")) {
                        aIMapBlock.isDestroyable = true;
                    }
                    if (go.CompareTag("NoneDestroyable")) {
                        aIMapBlock.isNoneDestroyable = true;
                    }
                    if (go.CompareTag("Collectable")) {
                        aIMapBlock.isCollectable = true;
                    }
                    Vector2Int mapPos = new(x, y);
                    if (go.CompareTag("Bomb")) {
                        BombController bombController = go.GetComponent<BombController>();
                        Bomb bomb = bombController.Bomb;
                        Explodable explodable = new(bomb, bombController.TimeToExplode);
                        pq.Add(explodable, bombController.TimeToExplode);
                        aIMapBlock.bomb = bomb;
                    }
                    if (go.CompareTag("Explode")) {
                        ExplodeController explodeController = go.GetComponent<ExplodeController>();
                        if (explodeController.TimeToExplode > 0) {
                            Explode explode = explodeController.Explode;
                            Explodable explodable = new(explode, explodeController.TimeToExplode);
                            pq.Add(explodable, explodeController.TimeToExplode);
                        }
                        aIMapBlock.AddExplodeInterval(0, explodeController.TimeToDestroy);
                    }
                }
                aiMap.Set(mapBlock, aIMapBlock);
            }
        }
        foreach (Player player in Player.livingPlayers) {
            Character character = player.Character;
            if (character.Id == playerId) continue;
            Vector2Int pos = new((int)Mathf.Floor(character.Position.x / 0.5f), (int)Mathf.Floor(character.Position.y / 0.5f));
            Vector2Int mapBlock = AI.PosToMapBlock(pos);
            AIMapBlock aIMapBlock = aiMap.Get(mapBlock);
            aIMapBlock.character = character;
            if (aIMapBlock.bomb == null) {
                Bomb bomb = new(mapBlock, character.BombPower);
                Explodable explodable = new(bomb, Bomb.explodeTime);
                pq.Add(explodable, Bomb.explodeTime);
                aIMapBlock.bomb = bomb;
            }
        }
        while (!pq.Empty()) {
            Explodable explodable = pq.Pop();
            if (explodable.Explode != null) {
                Explode explode = explodable.Explode;
                explode.CreateNextExplode((mapPos, powerLeft, direction) => {
                    AIMapBlock aIMapBlock = aiMap.Get(mapPos);
                    if (aIMapBlock.isNoneDestroyable) return;
                    if (aIMapBlock.isDestroyable) {
                        aIMapBlock.isDestroyable = false;
                        aIMapBlock.destroyableEnd = explodable.absExplodeTime;
                        return;
                    }
                    if (aIMapBlock.bomb != null) {
                        Bomb bomb = aIMapBlock.bomb;
                        pq.Remove(new(bomb));
                        BombExplode(bomb, explodable.absExplodeTime);
                    }
                    Explode explode = new Explode(mapPos, powerLeft, direction);
                    Explodable newExplodable = new(explode, explodable.absExplodeTime + Explode.explodeInterval);
                    pq.Add(newExplodable, newExplodable.absExplodeTime);
                    aIMapBlock.AddExplodeInterval(explodable.absExplodeTime, explodable.absExplodeTime + explode.ExistTime);
                });
            } else {
                Bomb bomb = explodable.Bomb;
                BombExplode(bomb, explodable.absExplodeTime);
            }
        }

        return aiMap;

        void BombExplode(Bomb bomb, float time) {
            Explode explode = new Explode(bomb.MapPos, bomb.BombPower, Direction.None);
            Explodable newExplodable = new(explode, time + Explode.explodeInterval);
            pq.Add(newExplodable, newExplodable.absExplodeTime);
            AIMapBlock aIMapBlock = aiMap.Get(bomb.MapPos);
            aIMapBlock.AddExplodeInterval(time, time + explode.ExistTime);
            aIMapBlock.bomb = null;
            aIMapBlock.bombEnd = time;
        }
    }
}

public class Explodable {
    public float absExplodeTime;
    private object explodable;
    public Bomb Bomb { get { return explodable is Bomb ? (Bomb)explodable : null; } }
    public Explode Explode { get { return explodable is Explode ? (Explode)explodable : null; } }

    public Explodable(Bomb bomb, float absExplodeTime = -1) {
        this.absExplodeTime = absExplodeTime;
        explodable = bomb;
    }

    public Explodable(Explode explode, float absExplodeTime = -1) {
        this.absExplodeTime = absExplodeTime;
        explodable = explode;
    }

    public override bool Equals(object obj) {
        if (obj is Explodable) {
            Explodable e = (Explodable)obj;
            return explodable == e.explodable;
        } else {
            return false;
        }
    }

    public override int GetHashCode() {
        return explodable.GetHashCode();
    }
}

public class AIMapBlock {
    private List<float> exStart = new();
    private List<float> exEnd = new();
    public Bomb bomb;
    public float bombEnd = -1;
    public bool isDestroyable = false;
    public float destroyableEnd = -1;
    public bool isNoneDestroyable = false;
    public Character character;
    public bool isCollectable = false;

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

    public List<float> TimesToEnter(float time) {
        List<float> result = new();
        result.Add(time);
        if (destroyableEnd > 0 && destroyableEnd > time) result.Add(destroyableEnd);
        for (int i = 0; i < exStart.Count; ++i) {
            if (exStart[i] > time) result.Add(exStart[i]);
            if (exEnd[i] > time) result.Add(exEnd[i]);
        }
        return result;
    }

    public void AddExplodeInterval(float start, float end) {
        int mergeStart = 0;
        while (mergeStart < exStart.Count && start > exEnd[mergeStart]) ++mergeStart;
        int mergeEnd = exStart.Count - 1;
        while (mergeEnd >= 0 && end < exStart[mergeEnd]) --mergeEnd;
        if (mergeStart <= mergeEnd) {
            start = start < exStart[mergeStart] ? start : exStart[mergeStart];
            end = end > exEnd[mergeEnd] ? end : exEnd[mergeEnd];
            exStart.RemoveRange(mergeStart, mergeEnd - mergeStart + 1);
            exEnd.RemoveRange(mergeStart, mergeEnd - mergeStart + 1);
        }
        exStart.Insert(mergeStart, start);
        exEnd.Insert(mergeStart, end);
    }

    public override string ToString() {
        string print = "";
        for (int i = 0; i < exStart.Count; ++i) {
            print += "[" + exStart[i] + "," + exEnd[i] + "], ";
        }
        return print;
    }
}
