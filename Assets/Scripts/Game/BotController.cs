using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class BotController : MonoBehaviour {
    private AI ai;
    private CharacterController characterController;
    private List<Instruction> currentInstructions = new();
    private Instruction currentInstruction;
    private List<Instruction> nextInstructions;
    private List<AIPredictionEvent> nextEvents;
    private Vector2Int pos;
    private int waitingDecisionId = -1;

    private void Awake() {
        characterController = GetComponent<CharacterController>();
        ai = new(characterController.character.Id);
        int x = (int)Mathf.Floor(characterController.transform.position.x / 0.5f);
        int y = (int)Mathf.Floor(characterController.transform.position.y / 0.5f);
        pos = new(x, y);
    }

    private void FixedUpdate() {
        if (!Static.networkVariables.gameRunning.Value || !characterController.alive.Value) return;
        RunInstruction(Time.fixedDeltaTime);
    }

    private bool IsValidInstruction(Instruction instruction) {
        if (instruction.waitTime == -1 && !instruction.putBomb) {
            Vector2Int curMapBlock = AI.PosToMapBlock(pos);
            Vector2Int nextMapBlock = AI.PosToMapBlock(instruction.pos);
            MapElement mapElement = Static.mapBlocks[nextMapBlock].element;
            if (mapElement is Bomb && !curMapBlock.Equals(nextMapBlock)) {
                return false;
            }
        }
        return true;
    }

    private void RetriveNextInstructions(float timeRemains) {
        Instruction finalInstruction = currentInstructions[^1];
        nextInstructions = null;
        List<AIPredictionEvent> events = nextEvents;
        nextEvents = null;
        ai.Decide(++waitingDecisionId, finalInstruction.pos, new(finalInstruction.time, timeRemains, events)).ContinueWith(
            (result) => {
                if (result.Result.Item1 != waitingDecisionId) {
                    //Debug.Log("decision overdue");
                    return;
                }
                nextInstructions = result.Result.Item2;
                nextEvents = result.Result.Item3;
            }
        );
    }

    private void RunInstruction(float time) {
        if (currentInstruction == null) {
            if (currentInstructions.Count == 0) {
                if (nextInstructions != null) {
                    currentInstructions = nextInstructions;
                } else {
                    currentInstructions = ai.DummyWaitInstruction(pos);
                }
                RetriveNextInstructions(time);
            }
            if (!IsValidInstruction(currentInstructions[0])) {
                currentInstructions = ai.DummyWaitInstruction(pos);
                RetriveNextInstructions(time);
            }
            currentInstruction = currentInstructions[0];
            currentInstructions.RemoveAt(0);
           // Debug.Log(currentInstruction);
        }
        float timeLeft = 0;
        if (currentInstruction.waitTime > 0) {
            characterController.Move(Direction.zero);
            float waitTime = currentInstruction.waitTime;
            if (waitTime > time) {
                currentInstruction.waitTime -= time;
            } else {
                timeLeft = time - waitTime;
                currentInstruction = null;
            }
        } else if (currentInstruction.putBomb) {
            characterController.PutBomb();
            currentInstruction = null;
            timeLeft = time;
        } else {
            Vector2Int targetPos = currentInstruction.pos;
            Vector2Int posChange = targetPos - pos;

            Direction direction;
            if (posChange.x == -1) direction = Direction.left;
            else if (posChange.x == 1) direction = Direction.right;
            else if (posChange.y == 1) direction = Direction.up;
            else /* down */ direction = Direction.down;

            Vector2 targetMapPos = AI.PosToMapPos(targetPos);
            float mapPosShouldChangeDistance = characterController.speed.Value * time;
            float mapPosCanChangeDistance = Mathf.Abs(direction.horizontal ? targetMapPos.x - transform.position.x : targetMapPos.y - transform.position.y);
            if (mapPosShouldChangeDistance > mapPosCanChangeDistance) {
                currentInstruction = null;
                pos = targetPos;
                characterController.Move(direction, mapPosCanChangeDistance);
                timeLeft = time - mapPosCanChangeDistance / characterController.speed.Value;
            } else {
                characterController.Move(direction, mapPosShouldChangeDistance);
            }
        }

        if (currentInstruction == null) {
            RunInstruction(timeLeft);
        }
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


