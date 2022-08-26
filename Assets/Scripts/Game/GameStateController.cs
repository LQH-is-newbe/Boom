using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameStateController : MonoBehaviour {
    private const float countDownTime = 3f;
    private const float gameOverMessageShowTime = 2.5f;
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
        gameObject.AddComponent<Timer>().Init(countDownTime, () => { Static.networkVariables.gameRunning.Value = true; });
        gameObject.AddComponent<Timer>().Init(firstAircraftTime, () => { CreateAircraft(); });
    }

    public void TestPlayerWins() {
        if (Character.characters.Count > 1) return;
        if (Character.characters.Count == 1) {
            Character winner = null;
            foreach (Character character in Character.characters.Values) {
                winner = character;
            }
            Static.networkVariables.ShowGameOverMessageClientRpc(Player.players[winner.Id].Name + " Wins!");
            if (!winner.IsNPC) {
                Static.networkVariables.PlaySoundEffectClientRpc("Win", Util.GetClientRpcParamsFor(winner.ClientId));
                Static.networkVariables.PlaySoundEffectClientRpc("Lose", Util.GetClientRpcParamsExcept(winner.ClientId));
            } else {
                Static.networkVariables.PlaySoundEffectClientRpc("Lose");
            }
        } else {
            Static.networkVariables.ShowGameOverMessageClientRpc("Game Over");
            Static.networkVariables.PlaySoundEffectClientRpc("Lose");
        }
        Static.networkVariables.gameRunning.Value = false;
        gameObject.AddComponent<Timer>().Init(gameOverMessageShowTime, () => { NewGame(); });
    }

    public void NewGame() {
        if (!Static.local && !Static.debugMode) Static.client.PostAsync("http://" + Static.httpServerAddress + ":8080/end-game", Static.portStringContent);
        Static.networkVariables.ReturnRoomClientRpc();
        NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
    }
}
