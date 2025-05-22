using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DoorDeath : MonoBehaviour
{
    [SerializeField] private string doorId;
    public string DoorId
    {
        get => doorId;
        private set => doorId = value;
    }
    private int counter;
    public int Counter => counter;

    public void Init()
    {
        DoorDeathEnemy[] enemies = FindObjectsByType<DoorDeathEnemy>(FindObjectsSortMode.None);
        counter = enemies.Count(enemies => enemies.DoorId == DoorId);
    }

    public int DecrementCounter()
    {
        return --counter;
    }

    public void OpenDoor()
    {
        gameObject.SetActive(false);
    }

    public void LockDoor()
    {
        gameObject.SetActive(true);
    }

    public void SetCounter(int value)
    {
        counter = value;
    }
}
