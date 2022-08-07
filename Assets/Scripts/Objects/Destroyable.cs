using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyable: MapElement {
    public Destroyable(Vector2Int mapBlock): base(mapBlock) { }

    public void Create(DestroyableController controller) {
        Static.mapBlocks[MapBlock].element = this;
        Static.destroyables.Add(this);
        Static.controllers[this] = controller;
        Static.totalDestroyableNum++;
    }

    public void Destroy() {
        ((DestroyableController)Static.controllers[this]).DestroyBlock();
        Static.mapBlocks[MapBlock].element = null;
        Static.destroyables.Remove(this);
        Static.controllers.Remove(this);
        Collectable.Type collectableType = Collectable.Creater.RandomCollectable();
        if (collectableType != null) {
            Collectable collectable = new(MapBlock, collectableType);
            collectable.Create();
        }
    }
}
