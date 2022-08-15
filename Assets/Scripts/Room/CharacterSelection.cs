using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelection : NetworkBehaviour {
    public GameObject characterSelectionUnitPrefab;
    private CharacterSelectionUnit selectedCharacter;
    public GameObject playerDisplayPrefab;
    private List<GameObject> playerDisplays = new();
    private bool isReady = false;
    public TMPro.TextMeshProUGUI readyButtonText;

    // server call
    public void Init() {
        for (int i = 0; i < Static.characters.Length; i++) {
            GameObject characterSelectionUnit = Instantiate(characterSelectionUnitPrefab);
            CharacterSelectionUnit controller = characterSelectionUnit.GetComponent<CharacterSelectionUnit>();
            controller.characterName.Value = new FixedString64Bytes(Static.characters[i]);
            characterSelectionUnit.GetComponent<NetworkObject>().Spawn();
            
        }
        for (int i = 0; i < 4; i++) {
            GameObject playerDisplay = Instantiate(playerDisplayPrefab);
            playerDisplay.GetComponent<RoomPlayerUI>().index.Value = i;
            playerDisplay.GetComponent<NetworkObject>().Spawn();
            playerDisplays.Add(playerDisplay);
            if (Player.players.ContainsKey(i)) {
                AddPlayer(Player.players[i]);
            }
        }
    }

    public void AddPlayer(Player player) {
        RoomPlayerUI playerDisplay = playerDisplays[player.Id].GetComponent<RoomPlayerUI>();
        if (player.IsNPC) playerDisplay.isBot.Value = true;
        playerDisplay.playerName.Value = new(player.Name);
        playerDisplay.isReady.Value = player.IsReady;
        if (player.CharacterName != null) playerDisplay.characterName.Value = new(player.CharacterName);
    }

    public void RemovePlayer(Player player) {
        RoomPlayerUI playerDisplay = playerDisplays[player.Id].GetComponent<RoomPlayerUI>();
        playerDisplay.isBot.Value = false;
        playerDisplay.isReady.Value = false;
        playerDisplay.characterName.Value = new("");
        playerDisplay.playerName.Value = new("");
    }

    // client call
    public void SelectCharacter(CharacterSelectionUnit selectedCharacter) {
        if (this.selectedCharacter != null) {
            this.selectedCharacter.Selected = false;
        }
        this.selectedCharacter = selectedCharacter;
        selectedCharacter.Selected = true;
        SelectCharacterServerRpc(NetworkManager.Singleton.LocalClientId, selectedCharacter.characterName.Value.Value);
    }

    public void SetReady() {
        isReady = !isReady;
        readyButtonText.text = isReady ? "Cancel" : "Ready";
        SetReadyServerRpc(NetworkManager.Singleton.LocalClientId, isReady);
    }

    // Server RPC
    [ServerRpc(RequireOwnership = false)]
    public void SelectCharacterServerRpc(ulong clientId, string characterName) {
        Player player = Player.clientPlayers[clientId];
        player.CharacterName = characterName;
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().characterName.Value = new(characterName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddBotServerRpc() {
        Player player = Player.CreatePlayer(true);
        if (player != null) {
            AddPlayer(player);
            if (!Static.singlePlayer) {
                Util.NotifyServerAddPlayer();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(ulong clientId, bool isReady) {
        Player player = Player.clientPlayers[clientId];
        player.IsReady = isReady;
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().isReady.Value = isReady;
        if (!isReady) return;
        foreach (ulong id in Player.clientPlayers.Keys) {
            if (!Player.clientPlayers[id].IsReady) return;
        }
        //TransitionClientRpc();
        if (!Static.singlePlayer) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/start-game", Static.roomIdJson);
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}
