using UnityEngine;

public class UIWalk : MonoBehaviour {
    public float speed;
    private RectTransform rectTransform;
    private float timer;

    private void Start() {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(-230f, 0);
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
    }

    public void Init(string characterName) {
        AnimatorOverrideController animatorOverrideController = new(GetComponent<Animator>().runtimeAnimatorController);
        animatorOverrideController["UI_walk"] = Resources.Load<AnimationClip>("Characters/" + characterName + "/Animations/UI_walk");
        GetComponent<Animator>().runtimeAnimatorController = animatorOverrideController;
    }

    private void Update() {
        timer += Time.deltaTime;
        float newX = -230f + timer * speed;
        rectTransform.anchoredPosition = new Vector2(newX, 0);
        if (newX > Screen.currentResolution.width + 230f) Destroy(gameObject);
    }
}
