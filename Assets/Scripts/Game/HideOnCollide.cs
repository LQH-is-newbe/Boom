using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnCollide : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.GetComponent<CharacterController>() != null && collision.GetComponent<CharacterController>().IsOwner) {
            foreach (SpriteRenderer child in GetComponentsInChildren<SpriteRenderer>()) {
                child.color = new Color32(255, 255, 255, 120);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.GetComponent<CharacterController>() != null && collision.GetComponent<CharacterController>().IsOwner) {
            foreach (SpriteRenderer child in GetComponentsInChildren<SpriteRenderer>()) {
                child.color = new Color32(255, 255, 255, 255);
            }
        }
    }
}
