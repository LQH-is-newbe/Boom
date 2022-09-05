using UnityEngine;

public class AIRunInstruction: TaskNode {
    private readonly AIContext aiContext;

    public AIRunInstruction(AIContext aiContext) {
        this.aiContext = aiContext;
    }

    public override State Evaluate() {
        if (aiContext.currentInstruction == null) {
            return State.FAILURE;
        }
        if (aiContext.currentInstruction.waitTime > 0) {
            aiContext.characterController.Move(Direction.zero);
            float waitTime = aiContext.currentInstruction.waitTime;
            if (waitTime > aiContext.timeRemain) {
                aiContext.currentInstruction.waitTime -= aiContext.timeRemain;
                aiContext.timeRemain = 0;
            } else {
                aiContext.timeRemain -= waitTime;
                aiContext.currentInstruction = null;
            }
        } else if (aiContext.currentInstruction.putBomb) {
            aiContext.characterController.PutBomb();
            aiContext.currentInstruction = null;
        } else {
            Vector2Int targetPos = aiContext.currentInstruction.pos;
            Vector2Int posChange = targetPos - aiContext.pos;

            Direction direction;
            if (posChange.x == -1) direction = Direction.left;
            else if (posChange.x == 1) direction = Direction.right;
            else if (posChange.y == 1) direction = Direction.up;
            else /* down */ direction = Direction.down;

            Vector2 targetMapPos = AIUtil.PosToMapPos(targetPos);
            float mapPosShouldChangeDistance = aiContext.characterController.speed.Value * aiContext.timeRemain;
            float mapPosCanChangeDistance = Mathf.Abs(direction.horizontal ? targetMapPos.x - aiContext.characterController.transform.position.x : targetMapPos.y - aiContext.characterController.transform.position.y);
            if (mapPosShouldChangeDistance > mapPosCanChangeDistance) {
                aiContext.currentInstruction = null;
                aiContext.pos = targetPos;
                aiContext.characterController.Move(direction, mapPosCanChangeDistance);
                aiContext.timeRemain -= mapPosCanChangeDistance / aiContext.characterController.speed.Value;
            } else {
                aiContext.characterController.Move(direction, mapPosShouldChangeDistance);
                aiContext.timeRemain = 0;
            }
        }
        return State.SUCCESS;
    }
}

public class Instruction {
    public Vector2Int pos;
    public float waitTime;
    public float time;
    public bool putBomb;

    public Instruction(Vector2Int pos, float time, float waitTime = -1, bool putBomb = false) {
        this.pos = pos;
        this.time = time;
        this.waitTime = waitTime;
        this.putBomb = putBomb;
    }

    public override string ToString() {
        if (waitTime > 0) {
            return "wait " + waitTime.ToString();
        } else if (putBomb) {
            return "putbomb";
        } else {
            return pos.ToString() + "at" + time.ToString();
        }
    }
}
