using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapChangeButton : MonoBehaviour, IPointerClickHandler {
    public bool next;
    public void OnPointerClick(PointerEventData eventData) {
        GameObject.Find("RoomUI").GetComponent<MapSelection>().ChangeMapServerRpc(next);
    }
}
