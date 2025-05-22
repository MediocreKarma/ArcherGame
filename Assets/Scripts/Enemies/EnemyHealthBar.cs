using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 barSize = new(0.4f, 0.05f);
    [SerializeField] private float verticalOffset = 1.0f;

    private EnemyAI enemyAI;
    private Transform cam;

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        cam = Camera.main.transform;
    }

    private void OnGUI()
    {
        if (enemyAI.hitpoints <= 0) return;
        if (Camera.main == null) return;

        Vector3 worldPosition = transform.position + Vector3.up * verticalOffset;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        // If behind camera, don't render
        if (screenPosition.z < 0) return;

        float healthPercent = Mathf.Clamp01((float)enemyAI.hitpoints / enemyAI.StartHitpoints);

        float width = barSize.x * 100;
        float height = barSize.y * 100;

        // Flip Y for GUI
        screenPosition.y = Screen.height - screenPosition.y;

        // Background (gray)
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(screenPosition.x - width / 2, screenPosition.y - height / 2, width, height), Texture2D.whiteTexture);

        // Foreground (green)
        GUI.color = Color.green;
        GUI.DrawTexture(new Rect(screenPosition.x - width / 2, screenPosition.y - height / 2, width * healthPercent, height), Texture2D.whiteTexture);

        // Reset GUI color
        GUI.color = Color.white;
    }
}