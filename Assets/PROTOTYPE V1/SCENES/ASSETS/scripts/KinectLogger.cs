using UnityEngine;
using System.IO;
using System;

public class KinectLogger : MonoBehaviour
{
    [Header("Settings")]
    public string userName = "DefaultUser";
    public float logInterval = 0.1f; // Log every 100ms
    public Transform avatarHeadBone; // Drag your Avatar's Head bone here
    
    private KinectManager kinectManager;
    private float timer;
    private string kinectFilePath;
    private string avatarFilePath;

    void Start()
    {
        kinectManager = KinectManager.Instance;
        PrepareLogFiles();
    }

    void PrepareLogFiles()
    {
        // Create a directory for logs if it doesn't exist
        string folderPath = Path.Combine(Application.dataPath, "InteractionLogs", userName);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Set separate file paths
        kinectFilePath = Path.Combine(folderPath, $"Kinect_Head_Raw_{timestamp}.csv");
        avatarFilePath = Path.Combine(folderPath, $"Avatar_Head_Final_{timestamp}.csv");

        // Write Headers
        File.WriteAllText(kinectFilePath, "Timestamp,Kinect_X,Kinect_Y,Kinect_Z\n");
        File.WriteAllText(avatarFilePath, "Timestamp,Avatar_X,Avatar_Y,Avatar_Z\n");
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= logInterval)
        {
            LogData();
            timer = 0;
        }
    }

    void LogData()
    {
        if (kinectManager == null) return;

        uint userId = kinectManager.GetPlayer1ID();
        if (userId == 0) return; // No player detected

        string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");

        // 1. LOG RAW KINECT COORDINATES
        if (kinectManager.IsJointTracked(userId, (int)KinectWrapper.NuiSkeletonPositionIndex.Head))
        {
            Vector3 kHeadPos = kinectManager.GetJointPosition(userId, (int)KinectWrapper.NuiSkeletonPositionIndex.Head);
            string kLine = $"{timeStr},{kHeadPos.x},{kHeadPos.y},{kHeadPos.z}\n";
            File.AppendAllText(kinectFilePath, kLine);
        }

        // 2. LOG AVATAR BONE COORDINATES (Separately)
        if (avatarHeadBone != null)
        {
            Vector3 aHeadPos = avatarHeadBone.position;
            string aLine = $"{timeStr},{aHeadPos.x},{aHeadPos.y},{aHeadPos.z}\n";
            File.AppendAllText(avatarFilePath, aLine);
        }
    }
}