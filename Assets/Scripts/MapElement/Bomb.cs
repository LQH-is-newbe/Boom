using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb: MapElement {
    public const float explodeTime = 2f;
    private int bombPower;
    private int createrId;
    public int BombPower { get { return bombPower; } }
    public int CreaterId { get { return createrId; } }

    public Bomb(Vector2Int mapPos, int bombPower, int createrId): base(mapPos) {
        this.bombPower = bombPower;
        this.createrId = createrId;
    }
}
