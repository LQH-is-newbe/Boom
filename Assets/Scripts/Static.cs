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
    public static float explodeInterval = 0.05f;
    public static float explodeEndExistTime = 0.3f;
    public static List<Collectable> collectables = new();
    public static List<Destroyable> destroyables = new();
    public static int totalDestroyableNum;

    // client
    public const string serverPublicAddress = "3.15.187.113";
    public static string playerName = "Duu";

    // both
    public static readonly HttpClient client = new HttpClient();
    public static string httpServerAddress;
    public static string[] characters = { "Trinny", "Mimmo", "Nou", "Duu" };
    public static string[] maps = { "Halloween", "Winter" };
    public static Vector2 characterColliderSize = new(0.6f, 0.4f);

    public static bool singlePlayer = false;
}

public class RoomId {
    public int roomId;
}
