using UnityEngine;

public class AIRepeat : DecoratorNode {
    private readonly AIContext aiContext;

    public AIRepeat(AIContext aiContext, BehaviourNode child): base(child) {
        this.aiContext = aiContext;
    }

    public override State Evaluate() {
        int times = 0;
        while (aiContext.timeRemain > 0) {
            if (times++ > 10) {
                Debug.Log("Repeat loop");
                break;
            }
            child.Evaluate();
        }
        return State.SUCCESS;
    }
}
