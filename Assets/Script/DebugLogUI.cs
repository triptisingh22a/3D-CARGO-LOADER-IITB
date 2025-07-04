using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DebugLogUI : MonoBehaviour
{
    public TextMeshProUGUI logOutputField; // Assign this in the Inspector
    public int maxLines = 30;

    private Queue<string> logQueue = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Optional: Filter only Debug logs
        if (type == LogType.Log)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string message = $"{timestamp} - {logString}";

            logQueue.Enqueue(message);

            // Keep only the last N lines
            while (logQueue.Count > maxLines)
                logQueue.Dequeue();

            // Update UI
            if (logOutputField != null)
            {
                logOutputField.text = string.Join("\n", logQueue.ToArray());
            }
        }
    }
}
