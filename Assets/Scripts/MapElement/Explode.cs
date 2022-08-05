using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Explode {
    public static float explodeInterval = 0.06f;
    public static float explodeEndExistTime = 0.3f;
    private Vector2Int mapPos;
    private float existTime;
    private int powerLeft;
    private Direction direction;
    public Direction Direction { get { return direction; } }
    public float ExistTime { get { return existTime; } }
    public int PowerLeft { get { return powerLeft; } }
    public Vector2Int MapPos { get { return mapPos; } }

    public Explode(Vector2Int mapPos, int powerLeft, Direction direction) {
        this.mapPos = mapPos;
        this.powerLeft = powerLeft;
        this.direction = direction;
        existTime = Static.explodeEndExistTime + (powerLeft - 1) * Static.explodeInterval;
    }

    public void CreateNextExplode(Action<Vector2Int, int, Direction> onExplodeCreate) {
        if (powerLeft <= 1) return;
        if (direction == Direction.None) {
            Direction[] directions = new Direction[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down };
            foreach (Direction direction in directions) {
                CreateNextExplode(direction, onExplodeCreate);
            }
        } else {
            CreateNextExplode(direction, onExplodeCreate);
        }
    }

    private void CreateNextExplode(Direction direction, Action<Vector2Int, int, Direction> onExplodeCreate) {
        Vector2Int newPos = mapPos + Vector2Int.directions[(int)direction];
        if (newPos.x < 0 || newPos.x >= Static.mapSize || newPos.y < 0 || newPos.y >= Static.mapSize) return;
        onExplodeCreate(newPos, powerLeft - 1, direction);
    }
}
