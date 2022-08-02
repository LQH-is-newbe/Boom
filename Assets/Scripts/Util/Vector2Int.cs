using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Vector2Int {
    public int x { get; set; }
    public int y { get; set; }
    public Vector2Int(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object obj) {
        if (obj is Vector2Int) {
            Vector2Int p = (Vector2Int)obj;
            return x == p.x && y == p.y;
        } else {
            return false;
        }
    }

    public override int GetHashCode() {
        return Tuple.Create(x, y).GetHashCode();
    }

    public static Vector2Int operator +(Vector2Int a, Vector2Int b) {
        return new Vector2Int(a.x + b.x, a.y + b.y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b) {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }

    public static Vector2 operator *(Vector2Int a, float multiplier) {
        return new Vector2(a.x * multiplier, a.y * multiplier);
    }

    public override string ToString() {
        return "(" + x + "," + y + ")";
    }
}
