using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transition : MonoBehaviour {
    public GameObject uiWalkPrefab;
    public float spawnInterval = 1;
    private float spawnTimer = 0;
    private int currentCharacterIndex = 0;

    private void Update() {
        if (spawnTimer <= 0) {
            GameObject uiWalk = Instantiate(uiWalkPrefab);
            uiWalk.GetComponent<UIWalk>().Init(Character.names[currentCharacterIndex++]);
            if (currentCharacterIndex == Character.names.Length) currentCharacterIndex = 0;
            uiWalk.transform.SetParent(gameObject.transform);
            spawnTimer = spawnInterval;
        }
        spawnTimer -= Time.deltaTime;
    }
}
