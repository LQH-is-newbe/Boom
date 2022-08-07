using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Explode {
    public const float explodeInterval = 0.06f;
    public const float explodeEndExistTime = 0.3f;

    public static GameObject explodePrefab = Resources.Load<GameObject>("Explode/Explode");

    private Vector2Int mapBlock;
    private float existTime;
    private int powerLeft;
    private Direction direction;
    public Direction Direction { get { return direction; } }
    public float ExistTime { get { return existTime; } }
    public int PowerLeft { get { return powerLeft; } }
    public Vector2Int MapBlock { get { return mapBlock; } }

    public Explode(Vector2Int mapBlock, int powerLeft, Direction direction) {
        this.mapBlock = mapBlock;
        this.powerLeft = powerLeft;
        this.direction = direction;
        existTime = Static.explodeEndExistTime + (powerLeft - 1) * Static.explodeInterval;
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

    public void CreatePrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> events, float time) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        predictionMapBlock.AddExplodeStart(time);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeExtend, time + explodeInterval, this), time + Explode.explodeInterval);
        events.Add(new(MapBlock, AIPredictionEvent.Type.ExplodeDestroy, time + ExistTime, this), time + ExistTime);
    }

    public void Extend() {
        CreateNextExplode((explode) => {
            MapElement next = Static.mapBlocks[explode.MapBlock].element;
            if (next is NoneDestroyable) return;
            if (next is Destroyable destroyable) {
                destroyable.Destroy();
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

    public void ExtendPrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> events, float time) {
        CreateNextExplode((explode) => {
            AIPredictionMapBlock predictionMapBlock = prediction.map[explode.MapBlock];
            if (predictionMapBlock.IsNoneDestroyable()) return;
            if (predictionMapBlock.IsDestroyable(time)) {
                predictionMapBlock.DestroyDestroyable(time);
                return;
            }
            Bomb bomb = predictionMapBlock.Bomb(time);
            if (bomb != null) {
                events.Remove(new(explode.MapBlock, AIPredictionEvent.Type.BombExplode));
                bomb.TriggerPrediction(prediction, events, time);
                return;
            }
            if (predictionMapBlock.IsCollectable(time)) {
                predictionMapBlock.DestroyDestroyable(time);
            }
            explode.CreatePrediction(prediction, events, time);
        });
    }

    public void Destroy() {
        Static.mapBlocks[MapBlock].explodes.Remove(this);
        Static.controllers.Remove(this);
    }

    public void DestroyPrediction(AIPrediction prediction, PriorityQueue<AIPredictionEvent, float> events, float time) {
        AIPredictionMapBlock predictionMapBlock = prediction.map[MapBlock];
        predictionMapBlock.AddExplodeEnd(time);
    }

    private void CreateNextExplode(Action<Explode> OnExplodeCreate) {
        if (powerLeft <= 1) return;
        if (direction == Direction.None) {
            Direction[] directions = new Direction[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down };
            foreach (Direction direction in directions) {
                CreateNextExplode(direction, OnExplodeCreate);
            }
        } else {
            CreateNextExplode(direction, OnExplodeCreate);
        }
    }

    private void CreateNextExplode(Direction direction, Action<Explode> onExplodeCreate) {
        Vector2Int newPos = mapBlock + Vector2Int.directions[(int)direction];
        if (newPos.x < 0 || newPos.x >= Static.mapSize || newPos.y < 0 || newPos.y >= Static.mapSize) return;
        onExplodeCreate(new(newPos, powerLeft - 1, direction));
    }
}
