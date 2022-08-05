using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map<T> {
    private int size;
    private T[,] map;

    public Map(int size) {
        this.size = size;
        map = new T[size, size];
    }

    public T this[Vector2Int pos] {
        get { return map[pos.x, pos.y]; }
        set { map[pos.x, pos.y] = value; }
    }

    public void Clear() {
        Array.Clear(map, 0, map.Length);
    }

    public override string ToString() {
        string result = "";
        for (int y = size - 1; y >= 0; --y) {
            for (int x = 0; x < size; ++x) {
                result += new Vector2Int(x, y) + ":" + (map[x, y] == null ? "null" : map[x, y]) + " ";
            }
            result += Environment.NewLine;
        }
        return result;
    }
}
