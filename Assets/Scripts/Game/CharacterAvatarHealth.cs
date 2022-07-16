using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;

public class CharacterAvatarHealth : NetworkBehaviour {
    public NetworkVariable<FixedString64Bytes> characterName = new();
    public NetworkVariable<float> y = new();
    public Image avatar;
    public TMPro.TextMeshProUGUI healthDisplay;

    public override void OnNetworkSpawn() {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(-230f, y.Value);
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        avatar.sprite = Resources.Load<Sprite>("Characters/" + characterName.Value.Value + "/Sprites/Idle1");
    }

    [ClientRpc]
    public void HealthChangeClientRpc(string healthText) {
        healthDisplay.text = healthText;
    }
}
