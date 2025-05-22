using System.Collections.Generic;
using UnityEngine;

public class DoorLever : Interactable
{
    [SerializeField] private List<GameObject> doors; // The door to be opened

    public enum Scale
    {
        X, Y
    }
    [SerializeField] private Scale scale; // The scale to be flipped

    public override void Trigger(Player interactor)
    {
        foreach (var door in doors)
        {
            door.SetActive(!door.activeSelf);
        }
        Vector3 newScale = transform.localScale;
        switch (scale)
        {
            case Scale.X:
                newScale.x *= -1;
                break;
            case Scale.Y:
                newScale.y *= -1;
                break;
            default:
                Debug.LogError("Invalid scale type");
                break;
        }
        transform.localScale = newScale;
    }
}