using UnityEngine;

public class AIResetTime: TaskNode{
    private readonly AIContext aiContext;

    public AIResetTime(AIContext aiContext) {
        this.aiContext = aiContext;
    }

    public override State Evaluate() {
        aiContext.timeRemain = Time.fixedDeltaTime;
        return State.SUCCESS;
    }
}
