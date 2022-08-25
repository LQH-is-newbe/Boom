using Newtonsoft.Json;
using TMPro;
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

    public void Create() {
        if (!canCreate) return;
        Util.StartTransition();
        var requestBody = new { message = roomMessage.text, hasPassword, password = passwordInput.GetComponent<TMP_InputField>().text };
        StartCoroutine(Util.WebRequestCoroutine(Util.WebRequest("http://" + Static.httpServerAddress + "/create-room", requestBody), (statusCode, responseBody) => {
            JoinRoomReturn joinRoomReturn = JsonConvert.DeserializeObject<JoinRoomReturn>(responseBody);
            Util.JoinRoom(joinRoomReturn);
        }));
    }

    public void Close() {
        gameObject.SetActive(false);
    }
}
