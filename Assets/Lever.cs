using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Lever : Interactable
{
    public UnityEvent triggerEvent;
    public UnityEvent resetEvent;
    public bool IsTriggered { get; set; } = false; // To prevent multiple triggers

    public override void Trigger(Player interactor)
    {
        triggerEvent.Invoke(); 
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
        interactPrompt.gameObject.SetActive(false);
        IsTriggered = true;
    }

    public void ResetTrigger()
    {
        resetEvent.Invoke();
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
        IsTriggered = false;
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
    private new void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsTriggered) return;
        base.OnTriggerEnter2D(collision);
    }
}
