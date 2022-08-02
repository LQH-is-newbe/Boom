using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;

public class CharacterAvatarHealth : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> characterName = new();
    public NetworkVariable<FixedString64Bytes> playerName = new();
    public NetworkVariable<int> playerId = new();
    public NetworkVariable<int> lives = new();
    public Image avatar;
    public List<GameObject> hearts;
    public TMPro.TextMeshProUGUI playerNameText;
    public TMPro.TextMeshProUGUI playerIdText;

    public override void OnNetworkSpawn() {
        lives.OnValueChanged += OnLivesChanged;
        transform.SetParent(GameObject.Find("CharactersAvatarHealthContent").transform, false);
        avatar.sprite = Resources.Load<Sprite>("Characters/" + characterName.Value.Value + "/Sprites/Idle1");
        playerNameText.text = playerName.Value.Value;
        playerIdText.text = playerId.Value + "P";
        OnLivesChanged(0, lives.Value);
    }

    private void OnLivesChanged(int previous, int current) {
        int i = 0;
        for (; i < current; ++i) {
            hearts[i].SetActive(true);
        }
        for (; i < 3; ++i) {
            hearts[i].SetActive(false);
        }
    }
}
