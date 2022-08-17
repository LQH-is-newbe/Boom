using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelection : NetworkBehaviour {
    private readonly NetworkVariable<bool> canStart = new(false);

    public GameObject singlePlayerButtons;
    public GameObject multiPlayerButtons;
    private GameObject startButton;
    public GameObject readyButton;
    public Sprite activeButton;
    public Sprite inActiveButton;
    public GameObject characterSelectionUnitPrefab;
    private CharacterSelectionUnit selectedCharacter;
    public GameObject playerDisplayPrefab;
    private readonly List<GameObject> playerDisplays = new();
    private bool isReady = false;

    public override void OnNetworkSpawn() {
        canStart.OnValueChanged += OnCanStartChanged;

        if (Static.singlePlayer) {
            singlePlayerButtons.SetActive(true);
            multiPlayerButtons.SetActive(false);
        } else {
            singlePlayerButtons.SetActive(false);
            multiPlayerButtons.SetActive(true);
        }
        startButton = GameObject.Find("StartButton");

        TestCanStart();
    }

    private void OnCanStartChanged(bool previous, bool current) {
        startButton.GetComponent<Image>().sprite = current ? activeButton : inActiveButton;
    }

    // server call
    public void Init() {
        for (int i = 0; i < Character.names.Length; i++) {
            GameObject characterSelectionUnit = Instantiate(characterSelectionUnitPrefab);
            CharacterSelectionUnit controller = characterSelectionUnit.GetComponent<CharacterSelectionUnit>();
            controller.characterName.Value = new FixedString64Bytes(Character.names[i]);
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
        playerDisplay.isBot.Value = player.IsNPC;
        playerDisplay.playerName.Value = new(player.Name);
        playerDisplay.isReady.Value = player.IsReady;
        playerDisplay.isTaken.Value = true;
        playerDisplay.characterName.Value = new(player.CharacterName == null ? "" : player.CharacterName);
        TestCanStart();
    }

    public void RemovePlayer(Player player) {
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().isTaken.Value = false;
        TestCanStart();
    }

    private void StartGame() {
        //TransitionClientRpc();
        if (!Static.singlePlayer && !Static.debugMode) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/start-game", Static.roomIdJson);
        ResetGameStateClientRpc();
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    private void TestCanStart() {
        if (Player.players.Count < 2) {
            canStart.Value = false;
            return;
        }
        if (Static.singlePlayer) {
            foreach (int id in Player.players.Keys) {
                if (Player.players[id].CharacterName == null) {
                    canStart.Value = false;
                    return;
                }
            }
            canStart.Value = true;
        } else {
            if (Player.clientPlayers.Count == 0) {
                canStart.Value = false;
                return;
            }
            foreach (ulong id in Player.clientPlayers.Keys) {
                if (!Player.clientPlayers[id].IsReady) {
                    canStart.Value = false;
                    return;
                }
            }
            canStart.Value = true;
        }
    }

    // client call
    public void SelectCharacter(CharacterSelectionUnit selectedCharacter) {
        if (this.selectedCharacter == null && !Static.singlePlayer) {
            readyButton.GetComponent<Image>().sprite = activeButton;
        }
        if (this.selectedCharacter != null) {
            this.selectedCharacter.Selected = false;
        }
        this.selectedCharacter = selectedCharacter;
        selectedCharacter.Selected = true;
        SelectCharacterServerRpc(NetworkManager.Singleton.LocalClientId, selectedCharacter.characterName.Value.Value);
    }

    public void SetReady() {
        if (!isReady && selectedCharacter == null) return;
        isReady = !isReady;
        readyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = isReady ? "Cancel" : "Ready";
        SetReadyServerRpc(NetworkManager.Singleton.LocalClientId, isReady);
    }

    // Server RPC
    [ServerRpc(RequireOwnership = false)]
    public void SelectCharacterServerRpc(ulong clientId, string characterName) {
        Player player = Player.clientPlayers[clientId];
        player.CharacterName = characterName;
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().characterName.Value = new(characterName);
        TestCanStart();
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddBotServerRpc() {
        Player player = Player.CreatePlayer(true);
        if (player != null) {
            AddPlayer(player);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc() {
        if (!canStart.Value) return;
        StartGame();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(ulong clientId, bool isReady) {
        Player player = Player.clientPlayers[clientId];
        player.IsReady = isReady;
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().isReady.Value = isReady;
        TestCanStart();
    }

    // Client RPC
    [ClientRpc]
    private void ResetGameStateClientRpc() {
        Static.hasObstacle.Clear();
    }
}
