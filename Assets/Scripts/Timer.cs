using UnityEngine;

public class Timer : MonoBehaviour {
    private float timer;
    private System.Action action;

    private void Update() {
        timer -= Time.deltaTime;
        if (timer <= 0) {
            Destroy(this);
            action();
        }
    }

    public void Init(float time, System.Action action) {
        this.action = action;
        timer = time;
    }

    public float TimeRemain() {
        return timer;
    }
}
