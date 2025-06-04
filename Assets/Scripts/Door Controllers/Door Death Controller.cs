using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorDeathController : MonoBehaviour
{
    private readonly Dictionary<string, DoorDeath> idToDoorDeathMapping = new();

    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    DoorDeath[] doors = FindObjectsByType<DoorDeath>(FindObjectsSortMode.None);
    //    foreach (DoorDeath door in doors)
    //    {
    //        if (!idToDoorDeathMapping.ContainsKey(door.DoorId))
    //        {
    //            idToDoorDeathMapping.Add(door.DoorId, door);
    //        }
    //    }
    //}

    public void Init()
    {
        DoorDeath[] doors = FindObjectsByType<DoorDeath>(FindObjectsSortMode.None);
        foreach (DoorDeath door in doors)
        {
            if (!idToDoorDeathMapping.ContainsKey(door.DoorId))
            {
                idToDoorDeathMapping.Add(door.DoorId, door);
            }
        }
    }

    public void UpdateCounter(string doorId)
    {
        if (idToDoorDeathMapping.ContainsKey(doorId))
        {
            var door = idToDoorDeathMapping[doorId];
            if (door != null && door.DecrementCounter() <= 0)
            {
                door.OpenDoor();
            }
        }
    }

    public List<DoorDeathData> GetSaveData()
    {
        List<DoorDeathData> list = new();
        foreach (var kvp in idToDoorDeathMapping)
        {
            if (kvp.Value != null)
            {
                list.Add(new DoorDeathData
                {
                    doorId = kvp.Key,
                    counter = kvp.Value.Counter
                });
            }
        }
        return list;
    }

    public void LoadSaveData(List<DoorDeathData> data)
    {
        foreach (var entry in data)
        {
            if (idToDoorDeathMapping.TryGetValue(entry.doorId, out var door))
            {
                door.LockDoor();
                door.SetCounter(entry.counter);
                if (door.Counter <= 0)
                {
                    door.OpenDoor();
                }
            }
        }
    }
}
