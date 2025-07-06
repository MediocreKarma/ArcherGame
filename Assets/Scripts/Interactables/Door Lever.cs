using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorLever : Interactable
{
    [SerializeField] private List<GameObject> doors; // The door to be opened

    public bool IsTriggered { get; set; } = false; // To prevent multiple triggers

    public override void Trigger(Player interactor)
    {
        if (IsTriggered)
        {
            return;
        }
        IsTriggered = true;
        foreach (var door in doors)
        {
            door.SetActive(false);
        }
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
        interactPrompt.gameObject.SetActive(false);
    }

    public void ResetTrigger()
    {
        if (!IsTriggered) 
            return;
        IsTriggered = false;
        foreach (var door in doors)
        {
            door.SetActive(true);
        }
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    private new void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsTriggered) return;
        base.OnTriggerEnter2D(collision);
    }

    public override void Init()
    {
        base.Init();
        if (interactAction != null)
        {
            string key = interactAction.action.GetBindingDisplayString();
            interactPrompt.text = $"Press [{key}]";
        }
    }
}