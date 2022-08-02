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
    public NetworkVariable<FixedString64Bytes> characterName = new();
    public NetworkVariable<FixedString64Bytes> playerName = new();
    public NetworkVariable<bool> isBot = new();
    public NetworkVariable<int> index = new();

    public override void OnNetworkSpawn() {
        transform.SetParent(GameObject.Find("PlayerRoomDisplays").transform, false);
        isBot.OnValueChanged += OnIsBotChanged;
        characterName.OnValueChanged += OnCharacterNameChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;
        animatorOverrideController = new AnimatorOverrideController(characterDisplay.GetComponent<Animator>().runtimeAnimatorController);
        animatorOverrideController["UI_static"] = Resources.Load<AnimationClip>("UI_none");
        characterDisplay.GetComponent<Animator>().runtimeAnimatorController = animatorOverrideController;
        OnIsBotChanged(false, isBot.Value);
        OnCharacterNameChanged("", characterName.Value);
        OnPlayerNameChanged("", playerName.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveBotServerRpc() {
        Player player = Player.players[index.Value];
        player.Remove();
        GameObject.Find("RoomUI").GetComponent<CharacterSelection>().RemovePlayer(player);
    }

    public void OnCharacterNameChanged(FixedString64Bytes previous, FixedString64Bytes current) {
        string characterName = current.Value;
        if (characterName != "") {
            animatorOverrideController["UI_idle"] = Resources.Load<AnimationClip>("Characters/" + characterName + "/Animations/UI_idle");
            characterDisplay.GetComponent<Animator>().SetBool("Active", true);
            characterDisplay.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        } else {
            characterDisplay.GetComponent<Animator>().SetBool("Active", false);
            characterDisplay.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
        }
    }

    public void OnPlayerNameChanged(FixedString64Bytes previous, FixedString64Bytes current) {
        playerNameText.text = current.Value;
    }

    public void OnIsBotChanged(bool previous, bool current) {
        removeBotButton.SetActive(isBot.Value);
    }
}
