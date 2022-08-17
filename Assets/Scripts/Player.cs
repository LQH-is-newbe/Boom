using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player {
    public static Dictionary<ulong, Player> clientPlayers = new();
    public static Dictionary<int, Player> players = new();
    private bool isNPC;
    public bool IsNPC { get { return isNPC; } }
    private int id;
    public int Id { get { return id; } }
    private string name;
    public string Name { get { return name; } }
    private string characterName;
    public string CharacterName { get { return characterName; } set { characterName = value; } }
    private ulong clientId;
    public ulong ClientId { get { return clientId; } }
    public bool IsReady { get; set; }

    public static Player CreatePlayer(bool isNPC, ulong clientId = 0, string clientName = "") {
        Player player = new();
        player.isNPC = isNPC;
        for (int i = 0; i < 4; ++i) {
            if (!players.ContainsKey(i)) {
                if (isNPC) {
                    player.name = "Bot " + (i + 1).ToString();
                    player.characterName = Character.names[Random.RandomInt(Character.names.Length)];
                    player.IsReady = true;
                } else {
                    clientPlayers[clientId] = player;
                    player.clientId = clientId;
                    player.name = clientName;
                    player.IsReady = false;
                }
                player.id = i;
                players[i] = player;
                if (!Static.debugMode && !Static.singlePlayer) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-join", Static.roomIdJson);
                return player;
            }
        }
        return null;
    }

    public void Remove() {
        players.Remove(id);
        if (!isNPC) clientPlayers.Remove(clientId);
        if (!Static.debugMode && !Static.singlePlayer) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/player-leave", Static.roomIdJson);
    }
}
