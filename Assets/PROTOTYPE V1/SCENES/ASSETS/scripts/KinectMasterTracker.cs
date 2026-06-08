using UnityEngine;
using TMPro;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CustomTriggerArea
{
    public string locationName = "Area Alpha";
    public Collider triggerCollider;
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;
    public string customMessage = "Entering Area...";
    
    [Header("Milestone Options")]
    public bool useMilestone = false;
    public float milestoneDuration = 5f;
    public GameObject milestonePopupPanel;
    public TextMeshProUGUI milestonePopupText;
    public string milestoneMessage = "Great job! Keep holding!";

    [HideInInspector] public float timeSpent = 0f;
    [HideInInspector] public bool isTracking = false;
    [HideInInspector] public bool milestoneTriggered = false;
}

[System.Serializable]
public class StateConditionalPopup
{
    public string targetLocationName;
    public string requiredState;
    public GameObject specificPopupPanel;
    public TextMeshProUGUI specificPopupText;
    public string specificMessage = "Perfect Posture in Area!";
}

public class KinectMasterTracker : MonoBehaviour
{
    [Header("Target Tracking Core")]
    [Tooltip("The specific tracked object collider allowed to trigger things (e.g. Kinect Hand)")]
    public Collider specificTargetCollider;

    [Header("Kinect State Tracking")]
    public Transform stateTargetObject;
    public float yThreshold = 1.5f;
    public string stateAbove = "Standing";
    public string stateBelow = "Crouching";
    private string currentUserState = "Unknown";

    [Header("Trigger Registry List")]
    public List<CustomTriggerArea> triggerAreas = new List<CustomTriggerArea>();

    [Header("State Conditional Rules")]
    public List<StateConditionalPopup> conditionalPopups = new List<StateConditionalPopup>();

    [Header("Global Logging Structure")]
    public string folderName = "Logs";
    public string csvFileName = "Kinect_Master_Database.csv";

    private Dictionary<Collider, CustomTriggerArea> colliderMap = new Dictionary<Collider, CustomTriggerArea>();

    void Start()
    {
        // Map colliders for efficient lookup
        foreach (var area in triggerAreas)
        {
            if (area.triggerCollider != null)
            {
                colliderMap[area.triggerCollider] = area;
                // Ensure collider configuration
                area.triggerCollider.isTrigger = true;
            }
        }
        Time.timeScale = 1f; // Ensure system is unfrozen post-login
    }

    void Update()
    {
        UpdateKinectState();
        ProcessTimers();
    }

    void UpdateKinectState()
    {
        if (stateTargetObject == null) return;
        currentUserState = (stateTargetObject.position.y >= yThreshold) ? stateAbove : stateBelow;
    }

    void ProcessTimers()
    {
        foreach (var area in triggerAreas)
        {
            if (area.isTracking)
            {
                area.timeSpent += Time.deltaTime;
                
                // Update basic UI text elements dynamically
                if (area.popupText != null)
                {
                    area.popupText.text = $"{area.customMessage}\nState: {currentUserState}\nTime: {area.timeSpent:F2}s";
                }

                // Check milestone criteria
                if (area.useMilestone && !area.milestoneTriggered && area.timeSpent >= area.milestoneDuration)
                {
                    area.milestoneTriggered = true;
                    TriggerMilestone(area);
                }
            }
        }
    }

    // Activated via independent collision handling system or standard trigger calls routed manually
    public void HandleObjectEnter(Collider trigger, Collider incoming)
    {
        if (incoming != specificTargetCollider) return;

        if (colliderMap.TryGetValue(trigger, out CustomTriggerArea area))
        {
            area.isTracking = true;
            area.timeSpent = 0f;
            area.milestoneTriggered = false;

            // Trigger base dynamic popups via fade dissolve
            StartCoroutine(FadeDissolve(area.popupPanel, true));
            CheckStateConditionals(area.locationName, true);
        }
    }

    public void HandleObjectExit(Collider trigger, Collider incoming)
    {
        if (incoming != specificTargetCollider) return;

        if (colliderMap.TryGetValue(trigger, out CustomTriggerArea area))
        {
            area.isTracking = false;

            StartCoroutine(FadeDissolve(area.popupPanel, false));
            if (area.useMilestone) StartCoroutine(FadeDissolve(area.milestonePopupPanel, false));
            CheckStateConditionals(area.locationName, false);

            SaveMetrics(area, incoming.name);
        }
    }

    void TriggerMilestone(CustomTriggerArea area)
    {
        if (area.milestonePopupText != null)
        {
            area.milestonePopupText.text = area.milestoneMessage;
        }
        StartCoroutine(FadeDissolve(area.milestonePopupPanel, true));
    }

    void CheckStateConditionals(string location, bool entering)
    {
        foreach (var rule in conditionalPopups)
        {
            if (rule.targetLocationName == location && rule.requiredState == currentUserState)
            {
                if (rule.specificPopupText != null) rule.specificPopupText.text = rule.specificMessage;
                StartCoroutine(FadeDissolve(rule.specificPopupPanel, entering));
            }
        }
    }

    // Coroutine managing transition processing on Material parameters cleanly
    IEnumerator FadeDissolve(GameObject panel, bool fadeIn)
    {
        if (panel == null) yield break;
        
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if(group == null) group = panel.AddComponent<CanvasGroup>();

        panel.SetActive(true);
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            group.alpha = fadeIn ? progress : (1f - progress);
            yield return null;
        }

        if (!fadeIn) panel.SetActive(false);
    }

    void SaveMetrics(CustomTriggerArea area, string objectName)
    {
        string username = LoginManager.CurrentUsername; 
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        string csvPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(csvPath)) Directory.CreateDirectory(csvPath);
        
        string fullPath = Path.Combine(csvPath, csvFileName);

        if (!File.Exists(fullPath))
        {
            string header = "Timestamp,Username,FinalState,Location,ObjectName,DurationSeconds,MilestoneReached";
            File.WriteAllText(fullPath, header + Environment.NewLine);
        }

        string entry = $"{timestamp},{username},{currentUserState},{area.locationName},{objectName},{area.timeSpent:F4},{area.milestoneTriggered}";
        File.AppendAllLines(fullPath, new string[] { entry });
    }
}