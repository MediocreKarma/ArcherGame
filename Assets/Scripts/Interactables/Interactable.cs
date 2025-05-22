using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected InputActionReference interactAction;

    protected TextMeshPro interactPrompt;
    protected Transform popupTransform;

    public string InteractableName { get; private set; }

    private Player player;

    public abstract void Trigger(Player interactor);

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }
        player.CurrentInteractable = this;
        interactPrompt.gameObject.SetActive(true);
    }

    protected void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }
        player.CurrentInteractable = null;
        interactPrompt.gameObject.SetActive(false);
    }
    public virtual void Init()
    {
        player = FindFirstObjectByType<Player>();
        InteractableName = gameObject.name;
        if (interactAction != null)
        {
            GameObject popup = new("Interact Popup");
            popup.transform.SetParent(transform);
            popup.transform.localPosition = new Vector3(0, -1.5f, 0);
            popupTransform = popup.transform;
            interactPrompt = popup.AddComponent<TextMeshPro>();
            interactPrompt.fontSize = 5f;
            interactPrompt.color = Color.white;
            interactPrompt.alignment = TextAlignmentOptions.Center;
            interactPrompt.sortingOrder = 100;
            interactPrompt.GetComponent<Renderer>().sortingLayerName = "UI";
            popup.SetActive(false);
        }
    }
}