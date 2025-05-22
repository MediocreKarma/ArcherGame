using UnityEngine;
using UnityEngine.InputSystem;

public class SavePoint : Interactable
{
    private SaveManager saveManager;
    public bool IsTriggered { get; set; }

    public override void Trigger(Player interactor)
    {
        if (IsTriggered) return;

        interactor.health = interactor.MaxHealth;
        IsTriggered = true;
        interactPrompt.gameObject.SetActive(false);
        saveManager.SaveGame(this);
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
            interactPrompt.text = $"Press [{key}] to save";
        }
        saveManager = FindFirstObjectByType<SaveManager>();
    }
}
