using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CountDown : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> mapName = new();

    [SerializeField]
    private TMPro.TextMeshProUGUI message;

    private Color color;
    private int nextCountDownNumber;
    private int clientsConnected = 0;

    private void Start() {
        if (IsClient) {
            gameObject.AddComponent<Timer>().Init(0.5f, () => { NotifyServerReadyServerRpc(); });
        }
    }

    private void ChangeCountDownMessage() {
        if (nextCountDownNumber > 0) {
            message.text = nextCountDownNumber.ToString();
            nextCountDownNumber--;
            gameObject.AddComponent<AlphaGradient>().Init(true, message.gameObject, 1);
            gameObject.AddComponent<Timer>().Init(1f, () => { ChangeCountDownMessage(); });
        } else {
            Static.audio.ChangeBackgroundMusic(mapName.Value.Value);
            message.text = "GO!";
            message.color = color;
            gameObject.AddComponent<Timer>().Init(1f, () => {
                gameObject.AddComponent<AlphaGradient>().Init(true, gameObject, 0.5f);
                gameObject.AddComponent<AlphaGradient>().Init(true, message.gameObject, 0.5f);
            });
            gameObject.AddComponent<Timer>().Init(1.5f, () => { Destroy(gameObject); });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerReadyServerRpc() {
        clientsConnected++;
        if (clientsConnected == Client.clients.Count) {
            StartCountDownClientRpc();
            GameObject.Find("GameStateController").GetComponent<GameStateController>().StartGame();
        }
    }

    [ClientRpc]
    private void StartCountDownClientRpc() {
        color = message.color;
        nextCountDownNumber = 3;
        Static.audio.PlaySoundEffect("CountDown");
        ChangeCountDownMessage();
    }
}
