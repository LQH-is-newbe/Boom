using UnityEngine;
using Unity.Netcode;

public class GameStarter : MonoBehaviour {
    [SerializeField]
    private GameObject uiPrefab;
    [SerializeField]
    private GameObject networkVariablesPrefab;

    private void Awake() {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetGameState();

        InitObjects();
    }

    private void ResetGameState() {
        Static.hasObstacle.Clear();
        Character.characters.Clear();
        Static.mapBlocks.Clear();
        for (int x = 0; x < Static.mapSize; ++x) {
            for (int y = 0; y < Static.mapSize; ++y) {
                Static.mapBlocks[new(x, y)] = new();
            }
        }
        Static.controllers.Clear();
        Static.collectables.Clear();
        Static.destroyables.Clear();
        Static.notExplodedDestroyableNum = 0;
        Static.totalDestroyableNum = 0;
    }

    private void InitObjects() {
        GameObject mapPrefab = Resources.Load<GameObject>("Maps/Map_" + Static.maps[Static.mapIndex]);
        GameObject map = Instantiate(mapPrefab, Vector2.zero, Quaternion.identity);
        map.GetComponent<NetworkObject>().Spawn(true);


        Destroyable.AssignDrops();

        GameObject ui = Instantiate(uiPrefab);
        ui.GetComponent<NetworkObject>().Spawn(true);

        GameObject networkVariables = Instantiate(networkVariablesPrefab);
        networkVariables.GetComponent<NetworkObject>().Spawn(true);

        int index = 0;
        foreach (Player player in Player.players.Values) {
            Character character = new(player);
            character.Create(index, player);
            ++index;
        }
    }
}
