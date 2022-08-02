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

    public void Set(Vector2Int pos, T value) {
        map[pos.x, pos.y] = value;
    }

    public T Get(Vector2Int pos) {
        return map[pos.x, pos.y];
    }

    public void Clear() {
        Array.Clear(map, 0, map.Length);
    }

    public override string ToString() {
        string result = "";
        for (int y = size - 1; y >= 0; --y) {
            for (int x = 0; x < size; ++x) {
                result += new Vector2Int(x, y) + ":" + (Get(new(x, y)) == null ? "null" : Get(new(x, y))) + " ";
            }
            result += Environment.NewLine;
        }
        return result;
    }
}
