using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blink : MonoBehaviour {
    private SpriteRenderer spriteRenderer;
    private float solidTime;
    private float transparentTime;
    private bool isFadeAway;
    private float timer;

    private void Update() {
        timer -= Time.deltaTime;
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(isFadeAway ? timer / solidTime : 1 - timer / transparentTime);
        spriteRenderer.color = color;
        if (timer <= 0) {
            isFadeAway = !isFadeAway;
            timer = isFadeAway ? solidTime : transparentTime;
        }
    }

    public void Init(SpriteRenderer spriteRenderer, float duration, float solidTime, float transparentTime) {
        this.spriteRenderer = spriteRenderer;
        this.solidTime = solidTime;
        this.transparentTime = transparentTime;
        isFadeAway = true;
        gameObject.AddComponent<Timer>().Init(duration, () => {
            Color color = spriteRenderer.color;
            color.a = 1;
            spriteRenderer.color = color;
            Destroy(this); 
        });
    }
}
