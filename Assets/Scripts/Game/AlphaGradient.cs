using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphaGradient : MonoBehaviour {
    private SpriteRenderer spriteRenderer;
    private bool isFadeAway;
    private float timer;
    private float time;

    private void Update() {
        timer -= Time.deltaTime;
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(isFadeAway ? timer / time : 1 - timer / time);
        spriteRenderer.color = color;
        if (timer <= 0) {
            Destroy(this);
        }
    }

    public void Init(bool isFadeAway, SpriteRenderer spriteRenderer, float time) {
        this.isFadeAway = isFadeAway;
        this.spriteRenderer = spriteRenderer;
        this.time = time;
        timer = time;
    }
}
