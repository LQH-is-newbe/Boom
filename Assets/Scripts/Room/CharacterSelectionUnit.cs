using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

public class CharacterSelectionUnit : NetworkBehaviour
    , IPointerEnterHandler
    , IPointerExitHandler
    , IPointerClickHandler {
    public GameObject avatar;
    private bool grey = true;
    private bool hover = false;
    public NetworkVariable<FixedString64Bytes> characterName = new();
    private CharacterSelection characterSelection;
    public TMPro.TextMeshProUGUI characterNameDisplay;
    private bool selected;
    public bool Selected { set { selected = value; } }

    private void Start() {
        SetGrey();
        characterSelection = GameObject.Find("RoomUI").GetComponent<CharacterSelection>();
    }

    private void Update() {
        bool pre_grey = grey;
        grey = !hover && !selected;
        if (pre_grey != grey) {
            if (grey) SetGrey();
            else SetNormal();
        }
    }

    public override void OnNetworkSpawn() {
        transform.SetParent(GameObject.Find("CharacterSelectionContent").transform, false);
        AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController(avatar.GetComponent<Animator>().runtimeAnimatorController);
        animatorOverrideController["UI_idle"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/UI_idle");
        animatorOverrideController["UI_static"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/UI_static");
        avatar.GetComponent<Animator>().runtimeAnimatorController = animatorOverrideController;
        characterNameDisplay.text = characterName.Value.Value;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        hover = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        hover = false;
    }

    public void OnPointerClick(PointerEventData eventData) {
        characterSelection.SelectCharacter(this);
    }

    private void SetGrey() {
        Color32 grey = new Color32(140, 140, 140, 255);
        GetComponent<Image>().color = grey;
        avatar.GetComponent<Image>().color = grey;
        avatar.GetComponent<Animator>().SetBool("Active", false);
    }

    private void SetNormal() {
        Color32 white = new Color32(255, 255, 255, 255);
        GetComponent<Image>().color = white;
        avatar.GetComponent<Image>().color = white;
        avatar.GetComponent<Animator>().SetBool("Active", true);
    }
}
