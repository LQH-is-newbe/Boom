using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyable: MapElement {
    public static readonly Sprite explodeSprite = Resources.Load<Sprite>("Destroyable/Sprite/Destroyed");

    public Collectable.Type collectableType;
    public bool exploding = false;

    public Destroyable(Vector2Int mapBlock): base(mapBlock) { }

    public void Create(DestroyableController controller) {
        Static.mapBlocks[MapBlock].element = this;
        Static.destroyables.Add(this);
        Static.controllers[this] = controller;
        Static.totalDestroyableNum++;
    }

    public void Destroy() {
        exploding = true;
        ((DestroyableController)Static.controllers[this]).DestroyBlock();
    }

    public void RemoveBlock() {
        Static.mapBlocks[MapBlock].element = null;
        Static.destroyables.Remove(this);
        Static.controllers.Remove(this);
        if (collectableType != null) {
            Collectable collectable = new(MapBlock, collectableType);
            collectable.Create();
        }
    }
}
