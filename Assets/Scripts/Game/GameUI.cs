using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour {
    private void Awake() {
        gameObject.name = "GameUI";
    }

    //private float updateTime = 0;
    //private float fixedUpdateTime = 0;

    //private void FixedUpdate() {
    //    fixedUpdateTime += Time.deltaTime;
    //}

    //private void Update() {
    //    updateTime += Time.deltaTime;
    //    Debug.Log(updateTime - fixedUpdateTime);
    //}
}
