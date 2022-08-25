using UnityEngine;

public class Transition : MonoBehaviour {
    public const float spawnInterval = 1;
    public GameObject uiWalkPrefab;
    private int currentCharacterIndex = 0;

    private void Awake() {
        CreateUIWalk();
    }

    private void CreateUIWalk() {
        GameObject uiWalk = Instantiate(uiWalkPrefab);
        uiWalk.GetComponent<UIWalk>().Init(Character.names[currentCharacterIndex++]);
        if (currentCharacterIndex == Character.names.Length) currentCharacterIndex = 0;
        uiWalk.transform.SetParent(gameObject.transform);
        gameObject.AddComponent<Timer>().Init(spawnInterval, () => { CreateUIWalk(); });
    }
}
