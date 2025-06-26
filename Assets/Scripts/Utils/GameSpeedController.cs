using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetTimeScale(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetTimeScale(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetTimeScale(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetTimeScale(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SetTimeScale(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            SetTimeScale(6);
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            SetTimeScale(7);
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            SetTimeScale(8);
        else if (Input.GetKeyDown(KeyCode.Alpha9))
            SetTimeScale(9);
        else if (Input.GetKeyDown(KeyCode.Alpha0))
            SetTimeScale(0);
    }

    void SetTimeScale(int key)
    {
        float newTimeScale = key == 0 ? 0f : 1f - (key * 0.1f);
        Time.timeScale = newTimeScale;
        Debug.Log($"Time scale set to {Time.timeScale}");
    }
}
