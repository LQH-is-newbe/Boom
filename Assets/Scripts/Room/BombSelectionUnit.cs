using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BombSelectionUnit : MonoBehaviour
    , IPointerClickHandler {
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Image image;

    private string bombName;

    public void Init(string bombName) {
        this.bombName = bombName;
        transform.SetParent(GameObject.Find("BombSelectionContent").transform, false);
        AnimatorOverrideController animatorOverrideController = new(animator.runtimeAnimatorController);
        animatorOverrideController["UI_idle"] = Resources.Load<AnimationClip>("Bomb/Animations/UI_idle");
        animatorOverrideController["UI_static"] = Resources.Load<AnimationClip>("Bomb/Animations/UI_static");
        animator.runtimeAnimatorController = animatorOverrideController;
        image.sprite = Resources.Load<Sprite>("Bomb/Sprites/" + bombName);
    }

    public void OnPointerClick(PointerEventData eventData) {
        GameObject.Find("RoomUI").GetComponent<CharacterSelection>().SelectBomb(bombName);
    }
}
