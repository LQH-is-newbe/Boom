using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapElement {
    private Vector2Int mapBlock;
    public Vector2Int MapBlock { get { return mapBlock; } }

    public MapElement(Vector2Int mapBlock) {
        this.mapBlock = mapBlock;
    }

    public MapElement Copy() {
        return (MapElement)MemberwiseClone();
    }
}
