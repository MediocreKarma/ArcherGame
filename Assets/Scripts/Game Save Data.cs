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
public class GameSaveData
{
    public PlayerData player;
    public List<EnemyData> enemies;
    public string triggeredSavePoint;
}