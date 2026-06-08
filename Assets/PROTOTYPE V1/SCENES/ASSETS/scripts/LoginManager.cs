using UnityEngine;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public static string CurrentUsername = "Guest"; // Accessible by all scripts
    
    [Header("UI References")]
    public GameObject loginPanel;
    public TMP_InputField nameField;

    void Awake()
    {
        // Pause the game time and show the cursor
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        loginPanel.SetActive(true);
    }

    public void ConfirmUsername()
    {
        if (!string.IsNullOrEmpty(nameField.text))
        {
            CurrentUsername = nameField.text;
            
            // Resume the game
            Time.timeScale = 1f;
            loginPanel.SetActive(false);
            
            // Optional: Lock cursor for gameplay
            // Cursor.lockState = CursorLockMode.Locked; 
            
            Debug.Log("User Logged In: " + CurrentUsername);
        }
    }
}