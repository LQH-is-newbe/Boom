using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public KeyCode leftKey, rightKey, upKey, downKey;
    public KeyCode putBombKey;
    private List<KeyCode> pressingDirKeys = new List<KeyCode>();
    private Character character;

    private void Start() {
        character = GetComponent<Character>();
    }

    private void Update() {
        if (!IsOwner) return;

        if (Input.GetKeyDown(leftKey)) pressingDirKeys.Add(leftKey);
        if (Input.GetKeyDown(rightKey)) pressingDirKeys.Add(rightKey);
        if (Input.GetKeyDown(upKey)) pressingDirKeys.Add(upKey);
        if (Input.GetKeyDown(downKey)) pressingDirKeys.Add(downKey);
        if (Input.GetKeyUp(leftKey)) pressingDirKeys.Remove(leftKey);
        if (Input.GetKeyUp(rightKey)) pressingDirKeys.Remove(rightKey);
        if (Input.GetKeyUp(upKey)) pressingDirKeys.Remove(upKey);
        if (Input.GetKeyUp(downKey)) pressingDirKeys.Remove(downKey);
        if (Input.GetKeyDown(putBombKey)) character.PutBombServerRpc();
        float horizontal = 0f, vertical = 0f;
        if (pressingDirKeys.Count > 0) {
            KeyCode dirKey = pressingDirKeys[^1];
            if (dirKey == leftKey) horizontal = -1f;
            if (dirKey == rightKey) horizontal = 1f;
            if (dirKey == upKey) vertical = 1f;
            if (dirKey == downKey) vertical = -1f;
        }
        character.Move(horizontal, vertical);

        //if (IsOwner) {
        //    if (Input.GetKeyDown(leftKey)) KeyServerRpc(0);
        //    if (Input.GetKeyDown(rightKey)) KeyServerRpc(1);
        //    if (Input.GetKeyDown(upKey)) KeyServerRpc(2);
        //    if (Input.GetKeyDown(downKey)) KeyServerRpc(3);
        //    if (Input.GetKeyUp(leftKey)) KeyServerRpc(4);
        //    if (Input.GetKeyUp(rightKey)) KeyServerRpc(5);
        //    if (Input.GetKeyUp(upKey)) KeyServerRpc(6);
        //    if (Input.GetKeyUp(downKey)) KeyServerRpc(7);
        //    if (Input.GetKeyDown(putBombKey)) KeyServerRpc(8);
        //}
        //if (NetworkManager.Singleton.IsServer) {
        //    float horizontal = 0f, vertical = 0f;
        //    if (pressingDirKeys.Count > 0) {
        //        KeyCode dirKey = pressingDirKeys[^1];
        //        if (dirKey == leftKey) horizontal = -1f;
        //        if (dirKey == rightKey) horizontal = 1f;
        //        if (dirKey == upKey) vertical = 1f;
        //        if (dirKey == downKey) vertical = -1f;
        //    }
        //    character.Move(horizontal, vertical);
        //}
    }

    //[ServerRpc]
    //public void KeyServerRpc(int type) {
    //    if (type == 0) pressingDirKeys.Add(leftKey);
    //    if (type == 1) pressingDirKeys.Add(rightKey);
    //    if (type == 2) pressingDirKeys.Add(upKey);
    //    if (type == 3) pressingDirKeys.Add(downKey);
    //    if (type == 4) pressingDirKeys.Remove(leftKey);
    //    if (type == 5) pressingDirKeys.Remove(rightKey);
    //    if (type == 6) pressingDirKeys.Remove(upKey);
    //    if (type == 7) pressingDirKeys.Remove(downKey);
    //    if (type == 8) character.PutBomb();
    //}
}
