using UnityEngine;

public class Destroyable: MapElement {
    public static readonly Sprite explodeSprite = Resources.Load<Sprite>("Destroyable/Sprite/Destroyed");
    private static readonly float dropRate = 0.5f;
    public const float explodeTime = 0.5f;

    public static void AssignDrops() {
        Collectable.Type[] types = Collectable.RandomTypes(Static.totalDestroyableNum, dropRate);
        for (int i = 0; i < Static.totalDestroyableNum; ++i) {
            if (types[i] != null) {
                Static.destroyables[i].collectableType = types[i];
            }
        }
    }

    public Collectable.Type collectableType;
    public bool exploding = false;

    public Destroyable(Vector2Int mapBlock): base(mapBlock) { }

    public void Create(DestroyableController controller) {
        Static.mapBlocks[MapBlock].element = this;
        Static.destroyables.Add(this);
        Static.controllers[this] = controller;
        Static.notExplodedDestroyableNum++;
        Static.totalDestroyableNum++;
    }

    public void Explode() {
        exploding = true;
        Static.notExplodedDestroyableNum--;
        ((DestroyableController)Static.controllers[this]).DestroyBlock();
    }

    public void Destroy() {
        Static.mapBlocks[MapBlock].element = null;
        Static.destroyables.Remove(this);
        Static.controllers.Remove(this);
        if (collectableType != null) {
            Collectable collectable = new(MapBlock, collectableType);
            collectable.Create();
        }
    }
}
