using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

public class Static {
    // server
    public static int n = 16;
    public static GameObject[,] map = new GameObject[n, n];
    public static Vector2[] playerPositions = { new Vector2(1.5f, 14f), new Vector2(14.5f, 1) };
    public static Dictionary<ulong, string> playerCharacters = new();
    public static Dictionary<ulong, string> playerNames = new();
    public static string passcode;
    public static StringContent roomIdJson;
    public static List<ulong> livingPlayers = new();

    // client
    public static string serverPublicAddress = "3.15.187.113";
    public static string playerName = "Duu";

    // both
    public static readonly HttpClient client = new HttpClient();
    public static string httpServerAddress;
    public static string[] characters = { "Trinny", "Mimmo" };

    public static bool singlePlayer = false;
}

public class RoomId {
    public int roomId;
}
