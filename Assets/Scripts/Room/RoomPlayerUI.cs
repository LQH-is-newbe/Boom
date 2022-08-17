using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

public class RoomPlayerUI : NetworkBehaviour {
    public GameObject characterDisplay;
    AnimatorOverrideController animatorOverrideController;
    public TMPro.TextMeshProUGUI playerNameText;
    public GameObject removeBotButton;
    public GameObject isBotIcon;
    public GameObject isPlayerIcon;
    public GameObject isNotReadyIcon;
    public GameObject isReadyIcon;
    public NetworkVariable<FixedString64Bytes> characterName = new();
    public NetworkVariable<FixedString64Bytes> playerName = new();
    public NetworkVariable<bool> isBot = new();
    public NetworkVariable<int> index = new();
    public NetworkVariable<bool> isReady = new();
    public NetworkVariable<bool> isTaken = new(false);

    public override void OnNetworkSpawn() {
        transform.SetParent(GameObject.Find("PlayerRoomDisplays").transform, false);
        isBot.OnValueChanged += OnIsBotChanged;
        characterName.OnValueChanged += OnCharacterNameChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;
        isReady.OnValueChanged += OnIsReadyChanged;
        isTaken.OnValueChanged += OnIsTakenChanged;
        animatorOverrideController = new AnimatorOverrideController(characterDisplay.GetComponent<Animator>().runtimeAnimatorController);
        animatorOverrideController["UI_static"] = Resources.Load<AnimationClip>("UI_none");
        characterDisplay.GetComponent<Animator>().runtimeAnimatorController = animatorOverrideController;
        OnIsBotChanged(false, isBot.Value);
        OnCharacterNameChanged("", characterName.Value);
        OnPlayerNameChanged("", playerName.Value);
        OnIsReadyChanged(false, isReady.Value);
        OnIsTakenChanged(false, isTaken.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveBotServerRpc() {
        Player player = Player.players[index.Value];
        player.Remove();
        GameObject.Find("RoomUI").GetComponent<CharacterSelection>().RemovePlayer(player);
    }

    private void SyncCharacterName() {
        if (isTaken.Value && characterName.Value != "") {
            animatorOverrideController["UI_idle"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/UI_idle");
            characterDisplay.GetComponent<Animator>().SetBool("Active", true);
            characterDisplay.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        } else {
            characterDisplay.GetComponent<Animator>().SetBool("Active", false);
            characterDisplay.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
        }
    }

    public void OnCharacterNameChanged(FixedString64Bytes previous, FixedString64Bytes current) {
        SyncCharacterName();
    }

    private void SyncPlayerName() {
        if (isTaken.Value) {
            playerNameText.text = playerName.Value.Value;
        } else {
            playerNameText.text = "";
        }
    }

    private void OnPlayerNameChanged(FixedString64Bytes previous, FixedString64Bytes current) {
        SyncPlayerName();
    }

    private void SyncIsBot() {
        if (isTaken.Value) {
            removeBotButton.SetActive(isBot.Value);
            isBotIcon.SetActive(isBot.Value);
            isPlayerIcon.SetActive(!isBot.Value);
        } else {
            removeBotButton.SetActive(false);
            isBotIcon.SetActive(false);
            isPlayerIcon.SetActive(false);
        }
    }

    private void OnIsBotChanged(bool previous, bool current) {
        SyncIsBot();
    }

    private void SyncIsReady() {
        if (Static.singlePlayer || !isTaken.Value) {
            isReadyIcon.SetActive(false);
            isNotReadyIcon.SetActive(false);
        } else {
            isReadyIcon.SetActive(isReady.Value);
            isNotReadyIcon.SetActive(!isReady.Value);
        }
    }

    private void OnIsReadyChanged(bool previous, bool current) {
        SyncIsReady();
    }

    private void OnIsTakenChanged(bool previous, bool current) {
        SyncCharacterName();
        SyncIsBot();
        SyncIsReady();
        SyncPlayerName();
    }
}
