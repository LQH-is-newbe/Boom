using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

public class GameStarter : MonoBehaviour {
    public GameObject uiPrefab;
    public GameObject characterPrefab;
    public GameObject characterAvatarHealthPrefab;

    private void Awake() {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetGameState();

        InitObjects();
    }

    private void ResetGameState() {
        Player.livingPlayers.Clear();
        foreach (Player player in Player.players.Values) {
            Player.livingPlayers.Add(player);
        }
        Static.map.Clear();
        Static.mapBlocks.Clear();
        for (int x = 0; x < Static.mapSize; ++x) {
            for (int y = 0; y < Static.mapSize; ++y) {
                Static.mapBlocks[new(x, y)] = new();
            }
        }
    }

    private void InitObjects() {
        GameObject mapPrefab = Resources.Load<GameObject>("Maps/Map_" + Static.maps[Static.mapIndex]);
        GameObject map = Instantiate(mapPrefab, Vector2.zero, Quaternion.identity);
        map.GetComponent<NetworkObject>().Spawn(true);

        GameObject ui = Instantiate(uiPrefab);
        ui.GetComponent<NetworkObject>().Spawn(true);

        int index = 0;
        foreach (Player player in Player.players.Values) {
            AddPlayerObjects(index, player);
            ++index;
        }
    }

    private void AddPlayerObjects(int index, Player player) {
        GameObject characterAvatarHealth = Instantiate(characterAvatarHealthPrefab);
        CharacterAvatarHealth characterAvatarHealthController = characterAvatarHealth.GetComponent<CharacterAvatarHealth>();
        characterAvatarHealthController.characterName.Value = new(player.CharacterName);
        characterAvatarHealthController.playerName.Value = new(player.Name);
        characterAvatarHealthController.lives.Value = 3;
        characterAvatarHealthController.playerId.Value = index + 1;
        if (!player.IsNPC) characterAvatarHealth.GetComponent<NetworkObject>().SpawnWithOwnership(player.ClientId);
        else characterAvatarHealth.GetComponent<NetworkObject>().Spawn();

        GameObject character = Instantiate(characterPrefab, Player.playerInitialPositions[index], Quaternion.identity);
        character.GetComponent<Character>().Init(player, characterAvatarHealthController);
        if (!player.IsNPC) {
            character.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.ClientId, true);
        } else {
            character.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
