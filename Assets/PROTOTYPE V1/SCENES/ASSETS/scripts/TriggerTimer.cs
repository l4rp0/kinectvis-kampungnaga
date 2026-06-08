using UnityEngine;
using TMPro;
using System.IO;
using System;

public class TriggerTimer : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject popupPanel;
    public TextMeshProUGUI uiText;

    [Header("State Tracking")]
    public Transform stateTarget; // Assign the bone/object to track (e.g., Head)
    public float yThreshold = 1.5f; // The height cutoff
    public string stateAbove = "Standing";
    public string stateBelow = "Crouching";
    private string currentUserState = "Unknown";

    [Header("Customization")]
    public string locationName = "Main Entrance"; 
    public string customMessage = "Status: Area Occupied";
    [Range(10, 100)]
    public float textScale = 36f;

    [Header("Timer Settings")]
    public bool resetOnExit = true;
    
    [Header("File Output Directories")]
    public string summaryFolder = "Logs/Summaries";
    public string rawDataFolder = "Logs/RawData";
    public string globalFolder = "Logs/global";
    public string csvFolder = "Logs/CSV_Database";

    [Header("Global Log Settings")]
    public bool saveToGlobalLog = true;
    public string globalLogName = "Master_Trigger_Log.txt";
    public string csvFileName = "Trigger_Database.csv";

    private float timeSpent = 0f;
    private bool isInside = false;
    private DateTime sessionStartTime;

    void Update()
    {
        uiText.fontSize = textScale;
        
        // Track the state every frame
        UpdateUserState();

        if (isInside)
        {
            timeSpent += Time.deltaTime;
            UpdateUI();
        }
    }

    void UpdateUserState()
    {
        if (stateTarget == null) return;

        // Compare Y position to threshold
        if (stateTarget.position.y >= yThreshold)
        {
            currentUserState = stateAbove;
        }
        else
        {
            currentUserState = stateBelow;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        isInside = true;
        sessionStartTime = DateTime.Now;
        if(popupPanel != null) popupPanel.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        isInside = false;
        if(popupPanel != null) popupPanel.SetActive(false);
        
        SaveAllData(other);

        if (resetOnExit)
        {
            timeSpent = 0f;
        }
    }

    void UpdateUI()
    {
        // Display state on the UI as well
        uiText.text = $"{customMessage}\nState: {currentUserState}\nTime: {timeSpent:F2}s";
    }

    void SaveAllData(Collider other)
    {
        string user = LoginManager.CurrentUsername;
        string timestamp = sessionStartTime.ToString("yyyy-MM-dd HH:mm:ss");
        string fileTimestamp = sessionStartTime.ToString("yyyy-MM-dd_HH-mm-ss");

        // 1. CSV (Includes State)
        SaveToCSV(user, other, timestamp);

        // 2. RAW DATA (Includes State)
        string rawPath = EnsureDirectory(rawDataFolder);
        string[] rawContent = {
            "--- RAW SESSION DATA ---",
            $"User: {user} | State: {currentUserState}",
            $"Location: {locationName}",
            $"Duration: {timeSpent:F4}",
            "-----------------------"
        };
        File.WriteAllLines(Path.Combine(rawPath, $"RAW_{user}_{locationName}_{fileTimestamp}.txt"), rawContent);

        // 3. GLOBAL LOG (Includes State)
        string logEntry = $"User: {user} | State: {currentUserState} | Duration: {timeSpent:F2}s | Location: \"{locationName}\"";
        
        if (saveToGlobalLog)
        {
            string globalPath = EnsureDirectory(globalFolder);
            File.AppendAllLines(Path.Combine(globalPath, globalLogName), new string[] { logEntry });
        }
    }

    void SaveToCSV(string user, Collider other, string timestamp)
    {
        string csvPath = EnsureDirectory(csvFolder);
        string fullPath = Path.Combine(csvPath, csvFileName);

        if (!File.Exists(fullPath))
        {
            // Added 'State' to CSV Header
            string header = "Timestamp,Username,State,Location,ObjectName,DurationSeconds";
            File.WriteAllText(fullPath, header + Environment.NewLine);
        }

        string csvEntry = $"{timestamp},{user},{currentUserState},{locationName},{other.name},{timeSpent:F4}";
        File.AppendAllLines(fullPath, new string[] { csvEntry });
    }

    string EnsureDirectory(string folderRelativePath)
    {
        string fullPath = Path.Combine(Application.dataPath, folderRelativePath);
        if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
        return fullPath;
    }
}