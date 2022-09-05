using System.Collections.Generic;

public abstract class BehaviourTree {
    protected BehaviourNode root;

    public void Tick() {
        root.Evaluate();
    }
}

public abstract class BehaviourNode {
    public enum State {
        RUNNING,
        SUCCESS,
        FAILURE,
    }

    public abstract State Evaluate();
}

public abstract class CompositeNode: BehaviourNode {
    protected List<BehaviourNode> children;

    public CompositeNode(List<BehaviourNode> children) {
        this.children = children;
    }
}

public abstract class DecoratorNode: BehaviourNode {
    protected BehaviourNode child;

    public DecoratorNode(BehaviourNode child) {
        this.child = child;
    }
}

public abstract class TaskNode: BehaviourNode {

}
 
public class Sequence : CompositeNode {
    public Sequence(List<BehaviourNode> children) : base(children) { }

    public override State Evaluate() {
        foreach (BehaviourNode node in children) {
            State state = node.Evaluate();
            if (state == State.FAILURE || state == State.RUNNING) {
                return state;
            }
        }
        return State.SUCCESS;
    }
}

public class Selector : CompositeNode {
    public Selector(List<BehaviourNode> children) : base(children) { }

    public override State Evaluate() {
        foreach (BehaviourNode node in children) {
            State state = node.Evaluate();
            if (state == State.SUCCESS || state == State.RUNNING) {
                return state;
            }
        }
        return State.FAILURE;
    }
}
