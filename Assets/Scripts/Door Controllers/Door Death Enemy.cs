using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class DoorDeathEnemy : MonoBehaviour
{

    [SerializeField] private string doorId;
    public string DoorId
    {
        get => doorId;
        private set => doorId = value;
    }


    private DoorDeathController controller;

    void Start()
    {
        controller = FindAnyObjectByType<DoorDeathController>();
        EnemyAI ai = GetComponent<EnemyAI>();
        ai.OnDeath += OnDeathUpdate;
    }

    void OnDeathUpdate()
    {
        if (controller != null)
        {
            controller.UpdateCounter(DoorId);
        }
    }
}
