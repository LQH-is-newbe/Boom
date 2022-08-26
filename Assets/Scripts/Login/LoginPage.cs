using Newtonsoft.Json;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginPage : MonoBehaviour {
    [SerializeField]
    private TMP_InputField soloPlayerName;
    [SerializeField]
    private TMP_InputField duoPlayerName1;
    [SerializeField]
    private TMP_InputField duoPlayerName2;
    [SerializeField]
    private GameObject localVsOnline;
    [SerializeField]
    private GameObject soloVsDuo;
    [SerializeField]
    private GameObject soloName;
    [SerializeField]
    private GameObject duoNames;
    [SerializeField]
    private GameObject howToPlay;
    [SerializeField]
    private GameObject testButton;
    [SerializeField]
    private Sprite activeButton;
    [SerializeField]
    private Sprite inActiveButton;
    [SerializeField]
    private Image soloStart;
    [SerializeField]
    private Image duoStart;
    [SerializeField]
    private GameObject welcomeMessage;

    private void Awake() {
        Static.playerNames.Clear();
        Player.players.Clear();
        Client.clients.Clear();
        if (Static.debugMode) testButton.SetActive(true);
        if (!Static.hasEnteredLoginPage) {
            welcomeMessage.SetActive(true);
            Static.hasEnteredLoginPage = true;
        }
    }

    public void Local() {
        localVsOnline.SetActive(false);
        soloVsDuo.SetActive(true);
        Static.local = true;
    }

    public void Online() {
        localVsOnline.SetActive(false);
        soloVsDuo.SetActive(true);
        Static.local = false;
    }

    public void HowToPlay() {
        localVsOnline.SetActive(false);
        howToPlay.SetActive(true);
    }

    public void QuitHowToPlay() {
        howToPlay.SetActive(false);
        localVsOnline.SetActive(true);
    }

    public void Solo() {
        soloVsDuo.SetActive(false);
        soloName.SetActive(true);
    }

    public void Duo() {
        soloVsDuo.SetActive(false);
        duoNames.SetActive(true);
    }

    public void ReturnLocalVsOnline() {
        soloVsDuo.SetActive(false);
        localVsOnline.SetActive(true);
    }

    public void SoloName() {
        if (soloPlayerName.text.Trim() == "") return;
        Static.playerNames.Add(soloPlayerName.text);
        StartGame();
    }

    public void OnSoloNameChanged() {
        if (soloPlayerName.text.Trim() == "") soloStart.sprite = inActiveButton;
        else soloStart.sprite = activeButton;
    }

    public void DuoNames() {
        if (duoPlayerName1.text.Trim() == "" || duoPlayerName2.text.Trim() == "") return;
        Static.playerNames.Add(duoPlayerName1.text);
        Static.playerNames.Add(duoPlayerName2.text);
        StartGame();
    }

    public void OnDuoNamesChanged() {
        if (duoPlayerName1.text.Trim() == "" || duoPlayerName2.text.Trim() == "") duoStart.sprite = inActiveButton;
        else duoStart.sprite = activeButton;
    }

    public void ReturnSoloVsDuo() {
        soloVsDuo.SetActive(true);
        soloName.SetActive(false);
        duoNames.SetActive(false);
    }

    private void StartGame() {
        if (Static.local) {
            Static.local = true;
            Client client = new(0, Static.playerNames);
            NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) => response.Approved = true;
            Util.SetNetworkTransport(false, "0.0.0.0", 7777);
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("Room", LoadSceneMode.Single);
        } else {
            SceneManager.LoadScene("Lobby");
        }
    }

    public void Test() {
        Static.playerNames.Add(duoPlayerName1.text);
        Static.playerNames.Add(duoPlayerName2.text);
        ConnectionData connectionData = new();
        connectionData.playerNames = Static.playerNames;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(connectionData));
        Util.SetNetworkTransport(true, "127.0.0.1", 7777);
        NetworkManager.Singleton.StartClient();
    }

    public void TurnOnSound() {
        Static.audio.Mute = false;
        welcomeMessage.SetActive(false);
    }

    public void TurnOffSound() {
        Static.audio.Mute = true;
        welcomeMessage.SetActive(false);
    }
}
