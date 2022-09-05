using System.Collections.Generic;
using UnityEngine;

public class AIBehaviourTree: BehaviourTree {
    public AIBehaviourTree(CharacterController characterController) {
        AIContext aiContext = new();
        aiContext.characterController = characterController;
        aiContext.playerId = characterController.character.Id;
        int x = (int)Mathf.Floor(characterController.transform.position.x / 0.5f);
        int y = (int)Mathf.Floor(characterController.transform.position.y / 0.5f);
        aiContext.pos = new(x, y);

        root = new Sequence(new List<BehaviourNode> {
            new AIResetTime(aiContext),
            new AIRepeat(aiContext,
                new Selector(new List<BehaviourNode> { 
                    new AIRunInstruction(aiContext),
                    new AIRetrieveInstruction(aiContext),
                    new AIDecide(aiContext),
                })
            ),
        });
    }


}

public class AIContext {
    public int playerId;
    public Vector2Int pos;
    public CharacterController characterController;
    public List<Instruction> currentInstructions = new();
    public Instruction currentInstruction;
    public float timeRemain;
}


