using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : MonoBehaviour {
    private const KeyCode 
        leftKey = KeyCode.LeftArrow,
        rightKey = KeyCode.RightArrow,
        upKey = KeyCode.UpArrow,
        downKey = KeyCode.DownArrow,
        putBombKey = KeyCode.Space;
    private List<KeyCode> pressingDirKeys = new List<KeyCode>();
    private CharacterController character;

    private void Awake() {
        character = GetComponent<CharacterController>();
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
        if (Input.GetKeyDown(putBombKey)) character.PutBomb();
    }

    private void FixedUpdate() {
        if (!Static.networkVariables.gameRunning.Value || !character.alive.Value) return;
        float horizontal = 0f, vertical = 0f;
        if (pressingDirKeys.Count > 0) {
            float posChange = character.speed.Value * Time.deltaTime;
            KeyCode dirKey = pressingDirKeys[^1];
            if (dirKey == leftKey) horizontal = -posChange;
            if (dirKey == rightKey) horizontal = posChange;
            if (dirKey == upKey) vertical = posChange;
            if (dirKey == downKey) vertical = -posChange;
        }
        character.Move(new Vector2(horizontal, vertical));
    }
}
