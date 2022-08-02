using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player {
    public static Dictionary<ulong, Player> clientPlayers = new();
    public static Dictionary<int, Player> players = new();
    public static List<Player> livingPlayers = new();
    public static Vector2[] playerInitialPositions = { 
        AI.PosToMapPos(new Vector2Int(13, 23)),
        AI.PosToMapPos(new Vector2Int(13, 26)),
        AI.PosToMapPos(new Vector2Int(1, 1)),
        AI.PosToMapPos(new Vector2Int(26, 1))};
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
    private Character character;
    public Character Character { get { return character; } set { character = value; } }

    public static Player CreatePlayer(bool isNPC, ulong clientId = 0, string clientName = "") {
        Player player = new();
        player.isNPC = isNPC;
        for (int i = 0; i < 4; ++i) {
            if (!players.ContainsKey(i)) {
                if (isNPC) {
                    player.name = "Bot " + (i + 1).ToString();
                    player.characterName = Static.characters[Random.RandomInt(Static.characters.Length)];
                } else {
                    clientPlayers[clientId] = player;
                    player.clientId = clientId;
                    player.name = clientName;
                }
                player.id = i;
                players[i] = player;
                return player;
            }
        }
        return null;
    }

    public void Remove() {
        livingPlayers.Remove(this);
        players.Remove(id);
        if (!isNPC) clientPlayers.Remove(clientId);
    }
}
