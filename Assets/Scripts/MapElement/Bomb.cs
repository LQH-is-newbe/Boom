using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb {
    public static float explodeTime = 2f;
    private Vector2Int mapPos;
    private int bombPower;
    public Vector2Int MapPos { get { return mapPos; } }
    public int BombPower { get { return bombPower; } }

    public Bomb(Vector2Int mapPos, int bombPower) {
        this.mapPos = mapPos;
        this.bombPower = bombPower;
    }
}
