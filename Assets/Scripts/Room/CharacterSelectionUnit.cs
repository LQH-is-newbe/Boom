using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSelectionUnit : MonoBehaviour
    , IPointerClickHandler {
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private TMPro.TextMeshProUGUI characterNameDisplay;

    private string characterName;

    public void Init(string characterName) {
        this.characterName = characterName;
        transform.SetParent(GameObject.Find("CharacterSelectionContent").transform, false);
        AnimatorOverrideController animatorOverrideController = new(animator.runtimeAnimatorController);
        animatorOverrideController["UI_idle"] = Resources.Load<AnimationClip>("Characters/" + characterName + "/Animations/UI_idle");
        animatorOverrideController["UI_static"] = Resources.Load<AnimationClip>("Characters/" + characterName + "/Animations/UI_static");
        animator.runtimeAnimatorController = animatorOverrideController;
        characterNameDisplay.text = characterName;
    }

    public void OnPointerClick(PointerEventData eventData) {
        GameObject.Find("RoomUI").GetComponent<CharacterSelection>().SelectCharacter(characterName);
    }
}
