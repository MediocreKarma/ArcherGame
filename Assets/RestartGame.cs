using System.Threading;
using TMPro;
using UnityEngine;

public class RestartGame : MonoBehaviour
{
    [SerializeField] private TextMeshPro youWonText;
    public void EnableRestart()
    {
        float elapsed = FindFirstObjectByType<Player>().ElapsedTime;
        youWonText.text = "You won!\n" +
                          "Time taken: " + elapsed.ToString("F2") + " seconds";
        SaveManager saveManager = FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
        {
            saveManager.RestoreDefaultSave();
        }
    }
}
