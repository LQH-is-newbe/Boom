using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;

public class MapSelection : NetworkBehaviour{
    public NetworkVariable<FixedString64Bytes> mapName = new();
    public NetworkVariable<bool> hasPrevious = new();
    public NetworkVariable<bool> hasNext = new();
    public Image map;
    public GameObject hasPreviousButton;
    public GameObject hasNextButton;

    public override void OnNetworkSpawn() {
        mapName.OnValueChanged += OnMapNameChanged;
        hasPrevious.OnValueChanged += OnHasPreviousChanged;
        hasNext.OnValueChanged += OnHasNextChanged;
        OnMapNameChanged("", mapName.Value);
        OnHasPreviousChanged(false, hasPrevious.Value);
        OnHasNextChanged(false, hasNext.Value);
    }

    private void OnMapNameChanged(FixedString64Bytes previous, FixedString64Bytes current) {
        map.sprite = Resources.Load<Sprite>("Maps/Sprites/" + current.Value);
    }

    private void OnHasPreviousChanged(bool previous, bool current) {
        hasPreviousButton.SetActive(current);
    }

    private void OnHasNextChanged(bool previous, bool current) {
        hasNextButton.SetActive(current);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeMapServerRpc(bool next) {
        if (next) {
            if (Static.mapIndex + 1 < Static.maps.Length) { // has next
                mapName.Value = new(Static.maps[++Static.mapIndex]);
                if (Static.mapIndex >= Static.maps.Length - 1) {
                    hasNext.Value = false;
                }
                hasPrevious.Value = true;
            }
        } else {
            if (Static.mapIndex - 1 >= 0) { // has previous
                mapName.Value = new(Static.maps[--Static.mapIndex]);
                if (Static.mapIndex <= 0) {
                    hasPrevious.Value = false;
                }
                hasNext.Value = true;
            }
        }
    }

    public void Previous() {
        ChangeMapServerRpc(false);
    }
    
    public void Next() {
        ChangeMapServerRpc(true);
    }
}
