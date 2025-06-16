using UnityEngine;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR;
using System.Drawing;

public class SaveManager : MonoBehaviour
{
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private Player player;
    private EnemyAI[] enemies;
    private DoorDeathController doorDeathController;

    private SavePoint[] savePoints;
    private DoorLever[] doorLevers;

    private string DefaultSavePath => Path.Combine(Application.persistentDataPath, "defaultSave.json");

    private Lever winningLever;

    public bool ResetTimer = false;

    GameSaveData data;

    public void SaveGame(SavePoint point)
    {
        data.player.position = player.transform.position;
        data.enemies.Clear();
        data.deathDoors = doorDeathController.GetSaveData();
        data.doorLevers.Clear();
        data.savePoints.Clear();
        data.triggeredSavePoint = point.gameObject.name;
        data.elapsedTime = player.ElapsedTime;

        foreach (var enemy in enemies)
        {
            data.enemies.Add(new EnemyData
            {
                isActivated = enemy.gameObject.activeSelf,
                isAlive = enemy.isAlive,
            });
        }

        foreach (var lever in doorLevers)
        {
            data.doorLevers.Add(new DoorLeverData
            {
                leverName = lever.InteractableName,
                isTriggered = lever.IsTriggered
            });
        }

        foreach (var savePoint in savePoints)
        {
            //Debug.Log($"Saving save point: {savePoint.InteractableName}, triggered: {savePoint.IsTriggered}");
            data.savePoints.Add(new SavePointData
            {
                savepointName = savePoint.InteractableName,
                isTriggered = savePoint.IsTriggered
            });
        }

        // previousSavePoint.IsTriggered = false;
        // previousSavePoint = point;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(SavePath, json);

        Debug.Log($"Game saved to {SavePath}");
    }

    public void LoadGame()
    {
        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        player.transform.SetPositionAndRotation(data.player.position, Quaternion.identity);
        player.health = player.MaxHealth;
        Arrow arrow = FindFirstObjectByType<Arrow>();
        arrow.Sticking.Unstick();
        arrow.RearmingBow();
        for (int i = 0; i < enemies.Length; ++i)
        {
            bool enabled = data.enemies[i].isActivated && data.enemies[i].isAlive;
            enemies[i].gameObject.SetActive(enabled);
            if (enabled)
            {
                enemies[i].Reset();
            }
        }

        doorDeathController.LoadSaveData(data.deathDoors);

        foreach (var lever in doorLevers)
        {
            var saved = data.doorLevers.Find(l => l.leverName.Equals(lever.InteractableName));
            if (saved != null)
            {
                if (!saved.isTriggered)
                {
                    lever.ResetTrigger();
                }
            }
        }
        foreach (var sp in savePoints)
        {
            var saved = data.savePoints.Find(s => s.savepointName.Equals(sp.InteractableName));
            if (saved != null)
            {
                sp.IsTriggered = saved.isTriggered;
            }
        }
        player.playerFirstInput = false;
        player.IsDead = false;
        var pRb = player.GetComponent<Rigidbody2D>();
        pRb.linearVelocity = Vector2.zero;
        pRb.angularVelocity = 0f;

        if (winningLever.IsTriggered)
        {
            winningLever.ResetTrigger();
        }
        if (ResetTimer)
        {
            player.ElapsedTime = 0f;
            ResetTimer = false;
        }
    }

    public void RestoreDefaultSave()
    {
        if (File.Exists(DefaultSavePath))
        {
            File.Copy(DefaultSavePath, SavePath, true);
        }
        else
        {
            Debug.LogError($"Default save file not found at {DefaultSavePath}");
        }
    }

    public void Init()
    {
        var winLeverObject = GameObject.Find("Win Lever");
        winningLever = winLeverObject.GetComponent<Lever>();
        enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.InstanceID);
        savePoints = FindObjectsByType<SavePoint>(FindObjectsSortMode.InstanceID);
        doorLevers = FindObjectsByType<DoorLever>(FindObjectsSortMode.InstanceID);
        player = FindFirstObjectByType<Player>();
        doorDeathController = FindFirstObjectByType<DoorDeathController>();
        const string defaultSavePoint = "Save Point #0";
        var defaultSave = GameObject.Find(defaultSavePoint).GetComponent<SavePoint>();
        data = new()
        {
            player = new PlayerData
            {
                position = player.transform.position,
            },
            enemies = new List<EnemyData>(),
            deathDoors = doorDeathController.GetSaveData(),
            doorLevers = new List<DoorLeverData>(),
            savePoints = new List<SavePointData>(),
            triggeredSavePoint = null,
            elapsedTime = player.ElapsedTime
        };
        SaveGame(defaultSave);
        File.Copy(SavePath, DefaultSavePath, true);
    }
}