using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    [Header("Target Settings")]
    public GameObject targetObject;
    public float moveStep = 0.1f; 

    [Header("Keyboard Settings")]
    public KeyCode positiveKey = KeyCode.D; // Default to 'D' for +X
    public KeyCode negativeKey = KeyCode.A; // Default to 'A' for -X

    void Update()
    {
        // Only allow movement if the game isn't paused (Login is finished)
        if (Time.timeScale > 0)
        {
            HandleKeyboardInput();
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(positiveKey))
        {
            MoveX(1);
        }
        else if (Input.GetKeyDown(negativeKey))
        {
            MoveX(-1);
        }
    }

    // This remains public so your UI Buttons still work!
    public void MoveX(float direction)
    {
        if (targetObject == null) return;

        Vector3 currentPos = targetObject.transform.position;
        currentPos.x += (direction * moveStep);
        targetObject.transform.position = currentPos;
    }
}