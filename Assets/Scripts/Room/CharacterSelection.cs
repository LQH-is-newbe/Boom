using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelection : NetworkBehaviour {
    private readonly NetworkVariable<bool> canStart = new(false);
    private readonly NetworkVariable<bool> canAddBot = new(true);

    [SerializeField]
    private GameObject singlePlayerButtons;
    [SerializeField]
    private GameObject multiPlayerButtons;
    [SerializeField]
    private GameObject readyButton;
    [SerializeField]
    private Sprite activeButton;
    [SerializeField]
    private Sprite inActiveButton;
    [SerializeField]
    private GameObject characterSelectionUnitPrefab;
    [SerializeField]
    private GameObject bombSelectionUnitPrefab;
    [SerializeField]
    private GameObject playerDisplayPrefab;

    // server
    private readonly List<GameObject> playerDisplays = new();
    // client
    private GameObject startButton;
    private GameObject addBotButton;
    private int selectingPlayerId;
    private ulong localClientId;
    private Dictionary<string, SelectionUnit> characterSelectionUnits;
    private Dictionary<string, SelectionUnit> bombSelectionUnits;

    public override void OnNetworkSpawn() {
        if (IsClient) {
            if (Static.local) {
                singlePlayerButtons.SetActive(true);
                multiPlayerButtons.SetActive(false);
            } else {
                singlePlayerButtons.SetActive(false);
                multiPlayerButtons.SetActive(true);
            }
            startButton = GameObject.Find("StartButton");
            addBotButton = GameObject.Find("AddBotButton");
            canStart.OnValueChanged += OnCanStartChanged;
            canAddBot.OnValueChanged += OnCanAddBotChanged;
            localClientId = NetworkManager.Singleton.LocalClientId;
            OnCanStartChanged(false, canStart.Value);
            OnCanAddBotChanged(false, canAddBot.Value);
            RequestSelectionOptionsServerRpc(localClientId);
            RequestReadyServerRpc(localClientId, false);
        }
    }

    // server call
    public void Init() {
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
        playerDisplay.isTaken.Value = true;
        playerDisplay.characterName.Value = new(player.CharacterName == null ? "" : player.CharacterName);
        TestCanStart();
        TestCanAddBot();
    }

    public void RemovePlayer(Player player) {
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().isTaken.Value = false;
        TestCanStart();
        TestCanAddBot();
    }

    private void StartGame() {
        //TransitionClientRpc();
        if (!Static.local && !Static.debugMode) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/start-game", Static.portStringContent);
        ResetGameStateClientRpc();
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    private void TestCanStart() {
        if (Player.players.Count < 2) {
            canStart.Value = false;
            return;
        }
        if (Static.local) {
            foreach (Player player in Player.players.Values) {
                if (player.CharacterName == null || player.BombName == null) {
                    canStart.Value = false;
                    return;
                }
            }
            canStart.Value = true;
        } else {
            if (Client.clients.Count == 0) {
                canStart.Value = false;
                return;
            }
            foreach (ulong id in Client.clients.Keys) {
                if (!Client.clients[id].IsReady) {
                    canStart.Value = false;
                    return;
                }
            }
            canStart.Value = true;
        }
    }

    private void TestCanAddBot() {
        canAddBot.Value = Player.players.Count < 4;
    }

    private void TestReady(ulong clientId, bool requestChange) {
        bool canReady = true;
        Client client = Client.clients[clientId];
        foreach (int playerId in client.playerIds) {
            if (Player.players[playerId].CharacterName == null || Player.players[playerId].BombName == null) canReady = false;
        }
        if (requestChange && (canReady && !client.IsReady || client.IsReady)) {
            client.IsReady = !client.IsReady;
            foreach (int playerId in client.playerIds) {
                playerDisplays[playerId].GetComponent<RoomPlayerUI>().isReady.Value = client.IsReady;
            }
            TestCanStart();
        }
        ResponseReadyClientRpc(canReady, client.IsReady, Util.GetClientRpcParamsFor(clientId));
    }

    // client call
    private void OnCanStartChanged(bool previous, bool current) {
        startButton.GetComponent<Image>().sprite = current ? activeButton : inActiveButton;
    }

    private void OnCanAddBotChanged(bool previous, bool current) {
        addBotButton.GetComponent<Image>().sprite = current ? activeButton : inActiveButton;
    }

    public void SelectCharacter(string characterName) {
        RequestSelectCharacterServerRpc(localClientId, selectingPlayerId, characterName);
    }

    public void SelectBomb(string bombName) {
        RequestSelectBombServerRpc(localClientId, selectingPlayerId, bombName);
    }

    public void SetReady() {
        RequestReadyServerRpc(localClientId, true);
    }

    // Server RPC
    [ServerRpc(RequireOwnership = false)]
    private void RequestSelectCharacterServerRpc(ulong clientId, int playerId, string characterName) {
        Player player = Player.players[playerId];
        player.CharacterName = characterName;
        playerDisplays[player.Id].GetComponent<RoomPlayerUI>().characterName.Value = new(characterName);
        TestCanStart();
        if (!player.IsNPC) TestReady(clientId, false);
        ResponseSelectCharacterClientRpc(playerId, characterName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSelectBombServerRpc(ulong clientId, int playerId, string bombName) {
        Player player = Player.players[playerId];
        player.BombName = bombName;
        TestCanStart();
        if (!player.IsNPC) TestReady(clientId, false);
        ResponseSelectBombClientRpc(playerId, bombName);
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
    public void RequestSelectionOptionsServerRpc(ulong clientId, int playerId = -1) {
        // TODO: check ownership
        if (playerId == -1) playerId = Client.clients[clientId].playerIds[0];
        if (!Player.players.TryGetValue(playerId, out Player player)) return;
        ResponseSelectionOptionsClientRpc(playerId, player.CharacterName, player.BombName, Util.GetClientRpcParamsFor(clientId));
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyServerRpc(ulong clientId, bool requestChange) {
        TestReady(clientId, requestChange);
    }

    // Client RPC
    [ClientRpc]
    private void ResetGameStateClientRpc() {
        Static.hasObstacle.Clear();
    }

    [ClientRpc]
    private void ResponseSelectionOptionsClientRpc(int playerId, string selectedCharacter, string selectedBomb, ClientRpcParams clientRpcParams = default) {
        selectingPlayerId = playerId;
        foreach (Transform child in GameObject.Find("CharacterSelectionContent").transform) {
            Destroy(child.gameObject);
        }
        characterSelectionUnits = new();
        foreach (string characterName in Character.names) {
            GameObject characterSelectionUnit = Instantiate(characterSelectionUnitPrefab);
            characterSelectionUnit.GetComponent<CharacterSelectionUnit>().Init(characterName);
            SelectionUnit controller = characterSelectionUnit.GetComponent<SelectionUnit>();
            characterSelectionUnits[characterName] = controller;
            if (characterName == selectedCharacter) {
                controller.Selected = true;
            }
        }
        foreach (Transform child in GameObject.Find("BombSelectionContent").transform) {
            Destroy(child.gameObject);
        }
        bombSelectionUnits = new();
        foreach (string bombName in Static.bombNames) {
            GameObject bombSelectionUnit = Instantiate(bombSelectionUnitPrefab);
            bombSelectionUnit.GetComponent<BombSelectionUnit>().Init(bombName);
            SelectionUnit controller = bombSelectionUnit.GetComponent<SelectionUnit>();
            bombSelectionUnits[bombName] = controller;
            if (bombName == selectedBomb) {
                controller.Selected = true;
            }
        }
    }

    [ClientRpc]
    private void ResponseReadyClientRpc(bool canReady, bool isReady, ClientRpcParams clientRpcParams = default) {
        readyButton.GetComponent<Image>().sprite = !isReady && !canReady ? inActiveButton : activeButton;
        readyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = isReady ? "Cancel" : "Ready";
    }

    [ClientRpc]
    private void ResponseSelectCharacterClientRpc(int playerId, string characterName) {
        if (selectingPlayerId != playerId) return;
        foreach (SelectionUnit unit in characterSelectionUnits.Values) {
            unit.Selected = false;
        }
        characterSelectionUnits[characterName].Selected = true;
    }

    [ClientRpc]
    private void ResponseSelectBombClientRpc(int playerId, string bombName) {
        if (selectingPlayerId != playerId) return;
        foreach (SelectionUnit unit in bombSelectionUnits.Values) {
            unit.Selected = false;
        }
        bombSelectionUnits[bombName].Selected = true;
    }
}
