using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.Net.Sockets;

public enum Direction {
    Left,
    Right,
    Up,
    Down,
    None
}

public class Util {
    public static ClientRpcParams GetClientRpcParamsExcept(ulong clientId) {
        List<ulong> sendIds = new();
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
            if (id != clientId) sendIds.Add(id);
        }
        return new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = sendIds.ToArray()
            }
        };
    }

    public static void AddClientObjects(int clientIndex, ulong clientId) {
        if (SceneManager.GetActiveScene().name != "Game") return;
        GameObject characterPrefab = Resources.Load<GameObject>("Characters/Character");
        GameObject character = GameObject.Instantiate(characterPrefab, Static.playerPositions[clientIndex], Quaternion.identity);
        Character characterController = character.GetComponent<Character>();
        characterController.characterName.Value = new FixedString64Bytes(Static.playerCharacters[clientId]);
        character.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        GameObject characterAvatarHealthPrefab = Resources.Load<GameObject>("UI/CharacterAvatarHealth");
        GameObject characterAvatarHealth = GameObject.Instantiate(characterAvatarHealthPrefab);
        CharacterAvatarHealth characterAvatarHealthController = characterAvatarHealth.GetComponent<CharacterAvatarHealth>();
        characterAvatarHealthController.characterName.Value = new FixedString64Bytes(Static.playerCharacters[clientId]);
        characterAvatarHealthController.y.Value = -160f - 265f * clientIndex;
        characterAvatarHealth.transform.SetParent(GameObject.FindGameObjectWithTag("UI").transform, false);
        characterAvatarHealth.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        characterController.characterAvatarHealth = characterAvatarHealthController;
    }

    public static void StartTransition() {
        GameObject transition = Resources.Load<GameObject>("UI/Transition");
        GameObject.Instantiate(transition);
    }

    public static void RunIfCliContains(string targetArg, Action func) {
        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args) {
            if (arg == targetArg) {
                func();
                return;
            }
        }
    }

    public static void RunIfCliContains(string targetArg, Action<string> func) {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; ++i) {
            if (args[i] == targetArg) {
                func(args[i+1]);
                return;
            }
        }
    }

    public static string GetLocalIPAddress() {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
