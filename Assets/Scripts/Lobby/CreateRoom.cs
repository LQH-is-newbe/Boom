using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoom : MonoBehaviour {
    [SerializeField]
    private TMP_InputField roomMessage;
    [SerializeField]
    private GameObject passwordInput;
    [SerializeField]
    private GameObject checkmark;
    [SerializeField]
    private Sprite activeButton;
    [SerializeField]
    private Sprite inActiveButton;
    [SerializeField]
    private GameObject createButton;

    private bool canCreate = false;
    private bool hasPassword = false;

    public void SwitchHasPassword() {
        hasPassword = !hasPassword;
        passwordInput.SetActive(hasPassword);
        checkmark.SetActive(hasPassword);
        TestCanCreate();
    }

    public void TestCanCreate() {
        canCreate = roomMessage.text.Trim() != "" && (!hasPassword || passwordInput.GetComponent<TMP_InputField>().text.Trim() != "");
        createButton.GetComponent<Image>().sprite = canCreate ? activeButton : inActiveButton;
    }

    public async void Create() {
        if (!canCreate) return;
        Util.StartTransition();
        var requestBody = new { message = roomMessage.text, hasPassword, password = passwordInput.GetComponent<TMP_InputField>().text };
        var stringContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        var response = await Static.client.PostAsync("http://" + Static.httpServerAddress + "/create-room", stringContent);
        var returnBody = JsonConvert.DeserializeObject<JoinRoomReturn>(await response.Content.ReadAsStringAsync());

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Static.httpServerAddress, returnBody.port);
        ConnectionData connectionData = new();
        connectionData.passcode = returnBody.passcode;
        connectionData.playerNames = Static.playerNames;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(connectionData));
        NetworkManager.Singleton.StartClient();
    }

    public void Close() {
        gameObject.SetActive(false);
    }
}
