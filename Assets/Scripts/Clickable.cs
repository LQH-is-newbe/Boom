using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Clickable : MonoBehaviour
    , IPointerClickHandler {
    public void OnPointerClick(PointerEventData eventData) {
        Static.audio.PlaySoundEffect("Click");
    }
}
