using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeAway : MonoBehaviour {
    private SpriteRenderer spriteRenderer;
    private float timer;
    private float time;

    private void Update() {
        if (timer > 0) {
            timer -= Time.deltaTime;
            Color color = spriteRenderer.color;
            color.a = timer / time;
            if (color.a < 0) {
                color.a = 0;
            }
            spriteRenderer.color = color;
        }
    }

    public void Init(SpriteRenderer spriteRenderer, float time) {
        this.spriteRenderer = spriteRenderer;
        this.time = time;
        timer = time;
    }
}
