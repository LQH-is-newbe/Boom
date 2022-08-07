using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoneDestroyable: MapElement {
    public NoneDestroyable(Vector2Int mapBlock) : base(mapBlock) { }

    public void Create() {
        Static.mapBlocks[MapBlock].element = this;
    }
}
