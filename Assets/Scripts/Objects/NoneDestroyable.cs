

public class NoneDestroyable: MapElement {
    public NoneDestroyable(Vector2Int mapBlock) : base(mapBlock) { }

    public void Create() {
        Static.mapBlocks[MapBlock].element = this;
    }
}
