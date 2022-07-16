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
    public Image background;
    public GameObject avatar;
    private bool grey = true;
    private bool hover = false;
    public NetworkVariable<FixedString64Bytes> characterName = new();
    public NetworkVariable<float> x = new();
    private CharacterSelection characterSelection;
    public TMPro.TextMeshProUGUI playerNameDisplay;
    public NetworkVariable<FixedString64Bytes> playerName = new();
    public NetworkVariable<bool> selected = new();

    private void Start() {
        SetGrey();
        characterSelection = transform.parent.GetComponent<CharacterSelection>();
    }

    private void Update() {
        bool pre_grey = grey;
        grey = !hover && !selected.Value;
        if (pre_grey != grey) {
            if (grey) SetGrey();
            else SetNormal();
        }
        playerNameDisplay.text = playerName.Value.Value;
    }

    public override void OnNetworkSpawn() {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(x.Value, 0);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController(avatar.GetComponent<Animator>().runtimeAnimatorController);
        animatorOverrideController["UI_idle"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/UI_idle");
        animatorOverrideController["UI_static"] = Resources.Load<AnimationClip>("Characters/" + characterName.Value.Value + "/Animations/UI_static");
        avatar.GetComponent<Animator>().runtimeAnimatorController = animatorOverrideController;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        hover = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        hover = false;
    }

    public void OnPointerClick(PointerEventData eventData) {
        characterSelection.SelectCharacterServerRpc(NetworkManager.Singleton.LocalClientId, characterName.Value.Value);
    }

    private void SetGrey() {
        Color32 grey = new Color32(140, 140, 140, 255);
        background.color = grey;
        avatar.GetComponent<Image>().color = grey;
        avatar.GetComponent<Animator>().SetBool("Moving", false);
    }

    private void SetNormal() {
        Color32 white = new Color32(255, 255, 255, 255);
        background.color = white;
        avatar.GetComponent<Image>().color = white;
        avatar.GetComponent<Animator>().SetBool("Moving", true);
    }
}
