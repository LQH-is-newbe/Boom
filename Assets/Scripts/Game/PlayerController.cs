using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : MonoBehaviour {
    private KeyCode leftKey, rightKey, upKey, downKey, putBombKey;
    private List<KeyCode> pressingDirKeys = new List<KeyCode>();
    private CharacterController character;

    private void Awake() {
        character = GetComponent<CharacterController>();
    }

    public void Init(int clientPlayerId) {
        if (Static.playerNames.Count == 1) {
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
            putBombKey = KeyCode.Space;
        } else {
            if (clientPlayerId == 0) {
                leftKey = KeyCode.A;
                rightKey = KeyCode.D;
                upKey = KeyCode.W;
                downKey = KeyCode.S;
                putBombKey = KeyCode.Space;
            } else if (clientPlayerId == 1) {
                leftKey = KeyCode.LeftArrow;
                rightKey = KeyCode.RightArrow;
                upKey = KeyCode.UpArrow;
                downKey = KeyCode.DownArrow;
                putBombKey = KeyCode.Keypad0;
            }
        }
    }

    private void Update() {
        if (Input.GetKeyDown(leftKey)) pressingDirKeys.Add(leftKey);
        if (Input.GetKeyDown(rightKey)) pressingDirKeys.Add(rightKey);
        if (Input.GetKeyDown(upKey)) pressingDirKeys.Add(upKey);
        if (Input.GetKeyDown(downKey)) pressingDirKeys.Add(downKey);
        if (Input.GetKeyUp(leftKey)) pressingDirKeys.Remove(leftKey);
        if (Input.GetKeyUp(rightKey)) pressingDirKeys.Remove(rightKey);
        if (Input.GetKeyUp(upKey)) pressingDirKeys.Remove(upKey);
        if (Input.GetKeyUp(downKey)) pressingDirKeys.Remove(downKey);
        if (Input.GetKeyDown(putBombKey) && Static.networkVariables.gameRunning.Value && !Static.paused) character.PutBomb();
    }

    private void FixedUpdate() {
        if (!Static.networkVariables.gameRunning.Value || !character.alive.Value) return;
        if (pressingDirKeys.Count > 0) {
            float posChange = character.speed.Value * Time.deltaTime;
            Direction direction;
            KeyCode dirKey = pressingDirKeys[^1];
            if (dirKey == leftKey) direction = Direction.left;
            else if (dirKey == rightKey) direction = Direction.right;
            else if (dirKey == upKey) direction = Direction.up;
            else /* down */ direction = Direction.down;
            character.Move(direction, posChange);
        } else {
            character.Move(Direction.zero);
        }
    }
}
