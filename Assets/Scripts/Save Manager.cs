using UnityEngine;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private Player player;
    private EnemyAI[] enemies;

    private SavePoint previousSavePoint;
    //public GameSaveData SaveData { get; private set; }

    public void SaveGame(SavePoint point)
    {
        GameSaveData data = new()
        {
            player = new PlayerData
            {
                position = player.transform.position,
            },
            enemies = new List<EnemyData>(),
        };

        foreach (var enemy in enemies)
        {
            data.enemies.Add(new EnemyData
            {
                isActivated = enemy.gameObject.activeSelf,
                isAlive = enemy.isAlive,
            });
        }
        previousSavePoint.IsTriggered = false;
        data.triggeredSavePoint = point.gameObject.name;
        previousSavePoint = point;

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
        arrow.RearmingBow();
        for (int i = 0; i < enemies.Length; ++i)
        {
            Debug.Log(enemies[i].gameObject.name + $" -> {data.enemies[i].isActivated && data.enemies[i].isAlive}");
            enemies[i].gameObject.SetActive(data.enemies[i].isActivated && data.enemies[i].isAlive);
            enemies[i].isAlive = data.enemies[i].isAlive;
            enemies[i].transform.SetPositionAndRotation(enemies[i].StartPosition, Quaternion.identity);
            enemies[i].hitpoints = enemies[i].StartHitpoints;
        }
        previousSavePoint = FindObjectsByType<SavePoint>(FindObjectsSortMode.None)
            .FirstOrDefault(sp => sp.gameObject.name == data.triggeredSavePoint);
        previousSavePoint.IsTriggered = true;
    }

    private void Start()
    {
        enemies = Resources.FindObjectsOfTypeAll<EnemyAI>()
            .Where(e => e.gameObject.scene.IsValid())
            .ToArray();
        player = FindFirstObjectByType<Player>();
        previousSavePoint = GameObject.Find("Save Point #0").GetComponent<SavePoint>();
        SaveGame(previousSavePoint);
    }
}