using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyable : MonoBehaviour {
    public GameObject collectablePrefab;

    public void Destroy() {
        Destroy(gameObject);
        Vector2 position = transform.position;
        Collectable.Creater.AttempCreateCollectable(collectablePrefab, position);
    }
}
