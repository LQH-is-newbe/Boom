using UnityEngine;

public class HideOnCollide : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        CharacterController characterController = collision.GetComponent<CharacterController>();
        if (characterController != null && characterController.IsOwner && !characterController.character.IsNPC) {
            foreach (SpriteRenderer child in GetComponentsInChildren<SpriteRenderer>()) {
                child.color = new Color32(255, 255, 255, 120);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        CharacterController characterController = collision.GetComponent<CharacterController>();
        if (characterController != null && characterController.IsOwner && !characterController.character.IsNPC) {
            foreach (SpriteRenderer child in GetComponentsInChildren<SpriteRenderer>()) {
                child.color = new Color32(255, 255, 255, 255);
            }
        }
    }
}
