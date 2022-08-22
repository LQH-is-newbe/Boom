using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

public class Static {
    // server
    public const int mapSize = 14;
    public static readonly Map<MapBlock> mapBlocks = new(mapSize);
    public static readonly Dictionary<object, object> controllers = new();
    public static string passcode;
    public static ushort port;
    public static StringContent portStringContent;
    public static int mapIndex = 0;
    public static readonly List<Collectable> collectables = new();
    public static readonly List<Destroyable> destroyables = new();
    public static int notExplodedDestroyableNum = 0;
    public static int totalDestroyableNum = 0;
    public const int targetFrameRate = 200;
    public static readonly string[] bombNames = { "Classic", "Pumpkinhead" };
    public static bool paused = false;

    // client
    public const string serverPublicAddress = "3.15.187.113";
    public static List<string> playerNames = new();

    // both
    public static readonly HttpClient client = new HttpClient();
    public static string httpServerAddress;
    public static string[] maps = { "Halloween", "Winter" };
    public static NetworkVariables networkVariables;
    public static Map<bool> hasObstacle = new(mapSize);

    public static bool local = false;
    public static bool debugMode = false;
}

public class RoomId {
    public int roomId;
}
