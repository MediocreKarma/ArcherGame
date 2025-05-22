using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PlayerData
{
    public Vector3 position;
}

[System.Serializable]
public class EnemyData
{
    public bool isActivated;
    public bool isAlive;
}

[System.Serializable]
public class DoorDeathData
{
    public string doorId;
    public int counter;
}

[System.Serializable]
public class DoorLeverData
{
    public string leverName;
    public bool isTriggered;
}

[System.Serializable]
public class SavePointData
{
    public string savepointName;
    public bool isTriggered;
}

[System.Serializable]
public class GameSaveData
{
    public PlayerData player;
    public List<EnemyData> enemies;
    public List<DoorDeathData> deathDoors;
    public List<DoorLeverData> doorLevers;
    public List<SavePointData> savePoints;
    public string triggeredSavePoint;
    public float elapsedTime;
}
