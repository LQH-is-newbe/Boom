using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Direction {
    public static readonly Direction left = new(0, true, false, Vector2Int.left, Vector2.left);
    public static readonly Direction right = new(1, true, false, Vector2Int.right, Vector2.right);
    public static readonly Direction up = new(2, false, true, Vector2Int.up, Vector2.up);
    public static readonly Direction down = new(3, false, true, Vector2Int.down, Vector2.down);
    public static readonly Direction zero = new(4, false, false, Vector2Int.zero, Vector2.zero);
    public static readonly Direction[] directions = { left, right, up, down, zero };

    public readonly int index;
    public bool horizontal;
    public bool vertical;
    public readonly Vector2Int Vector2Int;
    public readonly Vector2 Vector2;

    private Direction(int index, bool horizontal, bool vertical, Vector2Int Vector2Int, Vector2 Vector2) {
        this.index = index;
        this.horizontal = horizontal;
        this.vertical = vertical;
        this.Vector2Int = Vector2Int;
        this.Vector2 = Vector2;
    }
}
