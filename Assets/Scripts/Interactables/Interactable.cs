using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected InputActionReference interactAction;

    protected TextMeshPro interactPrompt;

    public abstract void Trigger(Player interactor);

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }
        if (!collision.TryGetComponent<Player>(out var player)) {
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
        collision.GetComponent<Player>().CurrentInteractable = null;
        interactPrompt.gameObject.SetActive(false);
    }

    protected void Start()
    {
        if (interactAction != null)
        {
            GameObject popup = new("SavePointPrompt");
            popup.transform.SetParent(transform);
            popup.transform.localPosition = new Vector3(0, -1.5f, 0);
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