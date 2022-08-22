using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlphaGradient : MonoBehaviour {
    private Material material;
    private TMPro.TextMeshProUGUI text;
    private Image image;
    private Color color;
    private bool isFadeAway;
    private float timer;
    private float time;

    private void Update() {
        timer -= Time.deltaTime;
        color.a = Mathf.Clamp01(isFadeAway ? timer / time : 1 - timer / time);
        if (material != null) material.color = color;
        else if (text != null) {
            text.color = color;
            //text.faceColor = color;
            //text.outlineColor = color;
        } else if (image != null) {
            image.color = color;
        }
        if (timer <= 0) {
            Destroy(this);
        }
    }

    public void Init(bool isFadeAway, GameObject changingObject, float time) {
        this.isFadeAway = isFadeAway;
        if (changingObject.GetComponent<Renderer>() != null) {
            material = changingObject.GetComponent<Renderer>().material;
            color = material.color;
        } else if (changingObject.GetComponent<TMPro.TextMeshProUGUI>() != null) {
            text = changingObject.GetComponent<TMPro.TextMeshProUGUI>();
            color = text.color;
        } else if (changingObject.GetComponent<Image>() != null) {
            image = changingObject.GetComponent<Image>();
            color = image.color;
        }
        this.time = time;
        timer = time;
    }
}
