using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameStateController : MonoBehaviour {
    private const float gameOverMessageShowTime = 2;
    private static readonly float firstAircraftTime = 120f / Player.players.Count;
    private static readonly float aircraftInterval = 15f;

    [SerializeField]
    private GameObject aircraftPrefab;

    private void CreateAircraft() {
        GameObject aircraft = Instantiate(aircraftPrefab, new Vector2(-5f, Random.RandomFloat() * Static.mapSize), Quaternion.identity);
        aircraft.GetComponent<NetworkObject>().Spawn(true);
        gameObject.AddComponent<Timer>().Init(aircraftInterval, () => { CreateAircraft(); });
    }

    public void StartGame() {
        gameObject.AddComponent<Timer>().Init(3, () => { Static.networkVariables.gameRunning.Value = true; });
        gameObject.AddComponent<Timer>().Init(firstAircraftTime, () => { CreateAircraft(); });
    }

    public void TestPlayerWins() {
        if (Character.characters.Keys.Count == 1) {
            foreach (int id in Character.characters.Keys) {
                ShowGameOverMessage(Player.players[id].Name + " Wins!");
            }
        } else if (Character.characters.Keys.Count == 0) {
            ShowGameOverMessage("Game Over");
        }
    }

    public void NewGame() {
        if (!Static.local && !Static.debugMode) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/end-game", Static.portStringContent);
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }

    private void ShowGameOverMessage(string message) {
        Static.networkVariables.ShowGameOverMessageClientRpc(message);
        Static.networkVariables.gameRunning.Value = false;
        gameObject.AddComponent<Timer>().Init(gameOverMessageShowTime, () => { NewGame(); });
    }
}
