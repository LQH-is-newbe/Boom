using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class BotController : MonoBehaviour {
    private AI ai;
    private Character character;
    private Rigidbody2D rigidbody2d;
    private List<Instruction> currentInstructions = new();
    private Instruction currentInstruction;
    private List<Instruction> nextInstructions;
    private List<AIPredictionEvent> nextEvents;
    private Vector2Int pos;
    private int waitingDecisionId = -1;

    private void Awake() {
        character = GetComponent<Character>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        ai = new(character.Id);
        int x = (int)Mathf.Floor(character.transform.position.x / 0.5f);
        int y = (int)Mathf.Floor(character.transform.position.y / 0.5f);
        pos = new(x, y);
    }

    private void FixedUpdate() {
        Vector2 newPos = RunInstruction(Time.deltaTime, rigidbody2d.position);
        character.Move(newPos - rigidbody2d.position);
    }

    private bool IsValidInstruction(Instruction instruction) {
        if (instruction.waitTime == -1 && !instruction.putBomb) {
            Vector2Int curMapBlock = AI.PosToMapBlock(pos);
            Vector2Int nextMapBlock = AI.PosToMapBlock(instruction.pos);
            GameObject go = Static.map[nextMapBlock];
            if (go != null && go.CompareTag("Bomb") && !curMapBlock.Equals(nextMapBlock)) {
                return false;
            }
        }
        return true;
    }

    private void RetriveNextInstructions() {
        Instruction finalInstruction = currentInstructions[currentInstructions.Count - 1];
        ai.Decide(++waitingDecisionId, finalInstruction.pos, new(finalInstruction.time, nextEvents)).ContinueWith(
            (result) => {
                if (result.Result.Item1 != waitingDecisionId) return;
                nextInstructions = result.Result.Item2;
                nextEvents = result.Result.Item3;
            }
        );
        nextInstructions = null;
        nextEvents = null;
    }

    private Vector2 RunInstruction(float time, Vector2 mapPos) {
        if (currentInstruction == null) {
            if (currentInstructions.Count == 0) {
                if (nextInstructions != null) {
                    currentInstructions = nextInstructions;
                } else {
                    currentInstructions = ai.DummyWaitInstruction(pos);
                }
                RetriveNextInstructions();
            }
            if (!IsValidInstruction(currentInstructions[0])) {
                currentInstructions = ai.DummyWaitInstruction(pos);
                RetriveNextInstructions();
            }
            currentInstruction = currentInstructions[0];
            currentInstructions.RemoveAt(0);
            Debug.Log(currentInstruction);
        }
        float timeLeft = 0;
        if (currentInstruction.waitTime > 0) {
            float waitTime = currentInstruction.waitTime;
            if (waitTime > time) {
                currentInstruction.waitTime -= time;
            } else {
                timeLeft = time - waitTime;
                currentInstruction = null;
            }
        } else if (currentInstruction.putBomb) {
            character.PutBomb();
            currentInstruction = null;
        } else {
            Vector2Int targetPos = currentInstruction.pos;
            Vector2 targetMapPos = AI.PosToMapPos(targetPos);
            Vector2 mapPosShouldChange = (targetPos - pos) * (character.Speed * time);
            Vector2 mapPosCanChange = targetMapPos - mapPos;
            if (mapPosShouldChange.sqrMagnitude > mapPosCanChange.sqrMagnitude) {
                currentInstruction = null;
                pos = targetPos;
                mapPos = targetMapPos;
                timeLeft = time - mapPosCanChange.magnitude / character.Speed;
            } else {
                mapPos += mapPosShouldChange;
            }
        }

        if (currentInstruction == null) {
            mapPos = RunInstruction(timeLeft, mapPos);
        }

        return mapPos;
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


