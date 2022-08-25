using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player {
    public static readonly Dictionary<int, Player> players = new();
    public bool IsNPC { get; }
    public int Id { get; }
    public string Name { get; }
    public string CharacterName { get; set; }
    public string BombName { get; set; }
    private ulong clientId;
    public ulong ClientId { get { return clientId; } }
    private int clientPlayerId;
    public int ClientPlayerId { get { return clientPlayerId; } }

    private Player(bool isNPC, int id, string name) {
        IsNPC = isNPC;
        Id = id;
        Name = name;
    }

    public static Player CreatePlayer(bool isNPC, Client client = null, string playerName = "") {
        for (int id = 0; id < 4; ++id) {
            if (!players.ContainsKey(id)) {
                Player player = new(isNPC, id, isNPC ? "Bot " + (id + 1).ToString() : playerName);
                if (isNPC) {
                    player.CharacterName = Character.names[Random.RandomInt(Character.names.Length)];
                    player.BombName = Static.bombNames[Random.RandomInt(Static.bombNames.Length)];
                } else {
                    player.clientId = client.Id;
                    player.clientPlayerId = client.playerIds.Count;
                    client.playerIds.Add(id);
                }
                players[id] = player;
                if (!Static.debugMode && !Static.local) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-join", Static.portStringContent);
                return player;
            }
        }
        return null;
    }

    public void Remove() {
        players.Remove(Id);
        Character.characters.Remove(Id);
        if (SceneManager.GetActiveScene().name == "Room") {
            GameObject.Find("RoomUI").GetComponent<CharacterSelection>().RemovePlayer(this);
        }
        if (!Static.debugMode && !Static.local) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-leave", Static.portStringContent);
    }
}
