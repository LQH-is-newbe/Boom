public class AIRetrieveInstruction: TaskNode {
    private readonly AIContext aiContext;

    public AIRetrieveInstruction(AIContext aiContext) {
        this.aiContext = aiContext;
    }

    public override State Evaluate() {
        if (aiContext.currentInstructions.Count == 0
            || !IsValidInstruction(aiContext.currentInstructions[0])) {
            return State.FAILURE;
        }
        aiContext.currentInstruction = aiContext.currentInstructions[0];
        aiContext.currentInstructions.RemoveAt(0);
        return State.SUCCESS;
    }

    private bool IsValidInstruction(Instruction instruction) {
        if (instruction.waitTime == -1 && !instruction.putBomb) {
            Vector2Int curMapBlock = AIUtil.PosToMapBlock(aiContext.pos);
            Vector2Int nextMapBlock = AIUtil.PosToMapBlock(instruction.pos);
            MapElement mapElement = Static.mapBlocks[nextMapBlock].element;
            if (mapElement is Bomb && !curMapBlock.Equals(nextMapBlock)) {
                return false;
            }
        }
        return true;
    }
}
