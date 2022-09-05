using System.Collections.Generic;
using UnityEngine;

public class BotController : MonoBehaviour {
    private CharacterController characterController;
    private AIBehaviourTree aiBehaviourTree;

    private void Awake() {
        characterController = GetComponent<CharacterController>();
        aiBehaviourTree = new(characterController);
    }

    private void FixedUpdate() {
        if (!Static.networkVariables.gameRunning.Value || !characterController.alive.Value) return;
        aiBehaviourTree.Tick();
    }
}


