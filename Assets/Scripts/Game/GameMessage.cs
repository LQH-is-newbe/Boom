using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameMessage : NetworkBehaviour {
    private Color32 color = new Color32(255, 255, 255, 0);
    private bool showing = false;
    public float changeTime = 1;
    private float hasChangedTime = 0;
    public TMPro.TextMeshProUGUI message;

    [ClientRpc]
    public void ShowMessageClientRpc(string message) {
        Debug.Log("show message client rpc");
        this.message.text = message;
        showing = true;
    }

    private void Update() {
        if (!showing) return;
        Debug.Log(hasChangedTime);
        hasChangedTime += Time.deltaTime;
        color.a = (byte)Mathf.Clamp(hasChangedTime / changeTime * 255, 0, 255);
        if (color.a == 255) showing = false;
        message.color = color;
    }
}
