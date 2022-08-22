using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Explode {
    public const float extendTime = 0.04f;
    public const float explodeEndExistTime = 0.3f;
    public const float fadeAwayTime = 0.1f;

    public static GameObject explodePrefab = Resources.Load<GameObject>("Explode/Explode");

    public Direction Direction { get; }
    public float ExistTime { get; }
    public int PowerLeft { get; }
    public Vector2Int MapBlock { get; }

    public Explode(Vector2Int mapBlock, int powerLeft, Direction direction) {
        MapBlock = mapBlock;
        PowerLeft = powerLeft;
        Direction = direction;
        ExistTime = explodeEndExistTime + (powerLeft - 1) * extendTime;
    }

    public Explode Copy() {
        return (Explode)MemberwiseClone();
    }

    public void Create() {
        Static.mapBlocks[MapBlock].explodes.Add(this);
        GameObject explode = UnityEngine.Object.Instantiate(explodePrefab, new(MapBlock.x, MapBlock.y), Quaternion.identity);
        ExplodeController controller = explode.GetComponent<ExplodeController>();
        Static.controllers[this] = controller;
        controller.explode = this;
        explode.GetComponent<NetworkObject>().Spawn(true);
    }

    public void CreatePrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent> events, float time) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        predictionMapBlock.AddExplodeStart(time);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeExtend, time + extendTime, this), time + extendTime);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeDestroy, time + ExistTime + AI.enterErrorTime, this), time + ExistTime + AI.enterErrorTime);
    }

    public void Extend() {
        CreateNextExplode((explode) => {
            MapElement next = Static.mapBlocks[explode.MapBlock].element;
            if (next is NoneDestroyable) return;
            if (next is Destroyable destroyable) {
                if (!destroyable.exploding) {
                    destroyable.Explode();
                }
                return;
            }
            if (next is Bomb bomb) {
                bomb.Trigger();
                return;
            }
            if (next is Collectable collectable) {
                collectable.Destroy();
            }
            explode.Create();
        });
    }

    public void ExtendPrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent> events, float time) {
        CreateNextExplode((explode) => {
            AIPredictionMapBlock predictionMapBlock = prediction.map[explode.MapBlock];
            if (predictionMapBlock.IsNoneDestroyable()) return;
            if (predictionMapBlock.IsDestroyable(time)) {
                if (predictionMapBlock.IsNotExplodedDestroyable(time)) {
                    predictionMapBlock.ExplodeDestroyable(time);
                    prediction.destroyableNum.ChangeOnLastValue(time, (num) => { return num - 1; });
                    events.Add(new(explode.MapBlock, AIPredictionEvent.Type.DestroyableDestroy, time + Destroyable.explodeTime + AI.enterErrorTime, new Destroyable(explode.MapBlock)), time + Destroyable.explodeTime + AI.enterErrorTime);
                }
                return;
            }
            Bomb bomb = predictionMapBlock.Bomb(time);
            if (bomb != null) {
                events.Remove(new(explode.MapBlock, AIPredictionEvent.Type.BombTrigger));
                bomb.TriggerPrediction(prediction, events, time);
                return;
            }
            if (predictionMapBlock.IsCollectable(time)) {
                predictionMapBlock.DestroyCollectable(time);
                prediction.collectableNum.ChangeOnLastValue(time, (num) => { return num - 1; });
            }
            explode.CreatePrediction(prediction, events, time);
        });
    }

    public void Destroy() {
        Static.mapBlocks[MapBlock].explodes.Remove(this);
        Static.controllers.Remove(this);
    }

    private void CreateNextExplode(Action<Explode> OnExplodeCreate) {
        if (PowerLeft <= 1) return;
        if (Direction == Direction.zero) {
            for (int i = 0; i < 4; ++i) {
                CreateNextExplode(Direction.directions[i], OnExplodeCreate);
            }
        } else {
            CreateNextExplode(Direction, OnExplodeCreate);
        }
    }

    private void CreateNextExplode(Direction direction, Action<Explode> onExplodeCreate) {
        Vector2Int newPos = MapBlock + direction.Vector2Int;
        if (newPos.x < 0 || newPos.x >= Static.mapSize || newPos.y < 0 || newPos.y >= Static.mapSize) return;
        onExplodeCreate(new(newPos, PowerLeft - 1, direction));
    }
}
