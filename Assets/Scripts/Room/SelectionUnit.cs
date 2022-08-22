using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

public class SelectionUnit : MonoBehaviour
    , IPointerEnterHandler
    , IPointerExitHandler {
    private static readonly Color32 grey = new(140, 140, 140, 255);
    private static readonly Color32 white = new(255, 255, 255, 255);

    [SerializeField]
    private Animator animator;


    private bool isActive = false;
    private bool hover = false;
    private bool selected;
    public bool Selected { set { selected = value; } }

    private void Awake() {
        SetInActive();
    }

    private void Update() {
        bool preIsActive = isActive;
        isActive = hover || selected;
        if (preIsActive != isActive) {
            if (isActive) SetActive();
            else SetInActive();
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        hover = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        hover = false;
    }

    private void SetInActive() {
        foreach (Image image in GetComponentsInChildren<Image>()) {
            image.color = grey;
        }
        animator.SetBool("Active", false);
    }

    private void SetActive() {
        foreach (Image image in GetComponentsInChildren<Image>()) {
            image.color = white;
        }
        animator.SetBool("Active", true);
    }
}
