using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

public class Static {
    // server
    public static int mapSize = 14;
    public static Map<MapBlock> mapBlocks = new(mapSize);
    public static Dictionary<object, object> controllers = new();
    public static string passcode;
    public static StringContent roomIdJson;
    public static int mapIndex = 0;
    public static List<Collectable> collectables = new();
    public static List<Destroyable> destroyables = new();
    public static int totalDestroyableNum;

    // client
    public const string serverPublicAddress = "3.15.187.113";
    public static string playerName = "Duu";

    // both
    public static readonly HttpClient client = new HttpClient();
    public static string httpServerAddress;
    public static string[] maps = { "Halloween", "Winter" };
    public static NetworkVariables networkVariables;
    public static Map<bool> hasObstacle = new(mapSize);

    public static bool singlePlayer = false;
}

public class RoomId {
    public int roomId;
}
