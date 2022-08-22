using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Client {
    public static readonly Dictionary<ulong, Client> clients = new();

    public readonly List<int> playerIds = new();
    public ulong Id { get; }
    public bool IsReady { get; set; }

    public Client(ulong id, List<string> playerNames) {
        IsReady = false;
        Id = id;
        clients[id] = this;
        for (int i = 0; i < playerNames.Count && i < 2; ++i) {
            Player.CreatePlayer(false, this, playerNames[i]);
        }
    }

    public void Remove() {
        clients.Remove(Id);
        foreach (int playerId in playerIds) {
            Player.players[playerId].Remove();
        }
    }
}
