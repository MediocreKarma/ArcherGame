using UnityEngine;
using System.IO;

public class ScreenshotController : MonoBehaviour
{
    private int screenshotIndex = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            TakeScreenshot();
        }
    }

    void TakeScreenshot()
    {
        string folderPath = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filename = $"screenshot_{screenshotIndex}.png";
        string fullPath = Path.Combine(folderPath, filename);

        ScreenCapture.CaptureScreenshot(fullPath);
        Debug.Log($"Screenshot saved to {fullPath}");

        screenshotIndex++;
    }
}