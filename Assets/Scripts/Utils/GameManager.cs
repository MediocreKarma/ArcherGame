using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private DoorDeathController doorController;

    private void Start()
    {
        //Cursor.visible = false;
        var interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        foreach (var interactable in interactables)
        {
            interactable.Init();
        }

        var deathDoors = FindObjectsByType<DoorDeath>(FindObjectsSortMode.None);
        foreach (var door in deathDoors)
        {
            door.Init();
        }
        doorController.Init();
        saveManager.Init();
    }
}