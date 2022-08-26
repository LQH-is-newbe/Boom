using System.Collections.Generic;
using System.Net.Http;

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
    public static readonly HttpClient client = new();

    // client
    public static Audio audio;
    public const string serverPublicAddress = "3.15.187.113";
    public static List<string> playerNames = new();
    public static bool hasEnteredLoginPage = false;

    // both
    public static string httpServerAddress;
    public static string[] maps = { "Halloween", "Winter" };
    public static NetworkVariables networkVariables;
    public static Map<bool> hasObstacle = new(mapSize);

    public static bool local = false;
    public static bool debugMode = false;
}
